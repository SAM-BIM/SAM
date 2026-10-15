"""The SAM-BIM Grasshopper component metadata contract - canonical shared validator.

Extracts the published parameter contract of every non-abstract Grasshopper component that declares a
`ComponentGuid`: the component name/nickname/category/subcategory, `LatestComponentVersion`, and for
every declared input and output the name, nickname, description, `Optional`, `Access`,
`ParamVisibility` and any persistent default.

    python param_contract.py --repo <path-to-repository>              # (re)write the manifest from source
    python param_contract.py --repo <path-to-repository> --check      # verify source against the committed manifest
    python param_contract.py --repo <path> --output <file>            # write the manifest somewhere else (dry run)

`--repo` defaults to the SAM repository this file lives in; `--output` defaults to
`<repo>/design/grasshopper-icons/param_contract.json`. Pointing `--repo` at another SAM-BIM
repository is how one shared copy serves every repository instead of a copy per repository: the
manifest is repository-specific data, the validator is not.

The contract exists because Grasshopper restores a component from the parameter archive stored in a
definition: a reopened component keeps the names the FILE has, not the names its class now declares.
Renaming a parameter therefore breaks every `Params.IndexOfInputParam`/`IndexOfOutputParam` lookup in
every existing definition, silently. The manifest pins the current names, and `--check` fails if any
of them, or the order, access, optionality, visibility or GUID, changes.

Naming-standard deviations (camelCase outputs, `_name_` inputs without a persistent default) are the
frozen legacy exceptions listed in `namingExceptions`: they are expected, not errors, but a NEW
deviation, or a listed exception that no longer deviates, fails the check so the list stays honest.
Descriptions are not frozen - they are checked for quality only.

## Why the scanner is embedded

The C# scanner below is the Grasshopper document-object scanner from SAM#166 (the design/grasshopper
icons tooling), in the generalised form first written for the `SAM_Psychrometrics` metadata pilot.
It is inlined rather than imported because the repository-local `inventory.py` copies have since
diverged (`SAM/design/grasshopper-icons/tools/inventory.py` is the older, SAM-only generation with a
different API; the pilot's copy is the generalised one). Inlining keeps this validator a single,
portable artifact that cannot silently change behaviour with whichever `inventory.py` happens to be
next to it. Consolidating the copies is recorded as a follow-up in
`docs/GH_COMPONENT_STANDARDS.md`; this file is the canonical copy and must not be modified per
repository.

See `docs/GH_COMPONENT_STANDARDS.md` for the standard this file enforces, and for the difference
between a change that needs a manifest rebuild and one that needs an owner-approved migration.
"""
import argparse
import fnmatch
import json
import os
import re
import sys

# --- C# scanner (SAM#166 lineage, generalised for the metadata pilot) --------------------------------

SKIP_DIRS = {"obj", "bin", ".git", "packages", "design", "build"}
CLASS_RE = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*((?:public|internal|private|protected|abstract|sealed|static|partial|\s)+)class\s+(\w+)(?:<[^>{]*>)?\s*(?::\s*([^{\n]+))?", re.M)
GUID_RE = re.compile(r"ComponentGuid\s*(?:=>|\{\s*get\s*\{\s*return)\s*new\s*(?:Guid\s*)?\(\s*\"([0-9a-fA-F-]+)\"")  # new Guid("..") and target-typed new("..")


def long_path(path):
    """A \\\\?\\ path on Windows, so a path over MAX_PATH can still be read."""
    path = os.path.abspath(path)
    return "\\\\?\\" + path if os.name == "nt" and not path.startswith("\\\\") else path


def base_args(text, pos):
    """Split the argument list of the `base(` call starting at/after pos (string- and paren-aware)."""
    i = text.index("(", pos) + 1
    args, depth, cur, instr = [], 0, "", False
    while i < len(text):
        ch = text[i]
        if instr:
            cur += ch
            if ch == "\\":
                cur += text[i + 1]; i += 1
            elif ch == '"':
                instr = False
        elif ch == '"':
            instr = True; cur += ch
        elif ch in "([{":
            depth += 1; cur += ch
        elif ch in ")]}":
            if depth == 0:
                args.append(cur.strip()); return args
            depth -= 1; cur += ch
        elif ch == "," and depth == 0:
            args.append(cur.strip()); cur = ""
        else:
            cur += ch
        i += 1
    return args


def lit(a):
    a = a.strip()
    if re.fullmatch(r'"(?:[^"\\]|\\.)*"', a):
        return a[1:-1]
    return "{" + a + "}"


def compile_removed(project_dir):
    """Files excluded from compilation by the project's `<Compile Remove="..."/>` globs."""
    patterns = []
    for f in os.listdir(project_dir):
        if not f.endswith(".csproj"):
            continue
        text = open(long_path(os.path.join(project_dir, f)), encoding="utf-8-sig", errors="replace").read()
        patterns += [m.replace("\\", "/") for m in re.findall(r'<Compile\s+Remove="([^"]+)"', text)]

    def removed(rel):
        rel = rel.replace("\\", "/")
        return any(fnmatch.fnmatch(rel, p) or fnmatch.fnmatch(rel, p.replace("**/", "")) for p in patterns)

    return removed


def gh_projects(repo):
    """{project dir (abs): project name} for every non-test .csproj that references Grasshopper."""
    result = {}
    for root, dirs, files in os.walk(repo):
        dirs[:] = sorted(d for d in dirs if d not in SKIP_DIRS)
        for f in files:
            if not f.endswith(".csproj") or "Test" in f:
                continue
            text = open(long_path(os.path.join(root, f)), encoding="utf-8-sig", errors="replace").read()
            if re.search(r"Grasshopper", text, re.I) and re.search(r'Include="Grasshopper"|Grasshopper\.dll|GH_IO|"Grasshopper"', text):
                result[root] = f[:-7]
    return result


# --- C# lexing helpers ------------------------------------------------------------------------------

def take_balanced(text, index):
    """Return the text between text[index] ('(' or '{') and its matching closer, and the index after it."""
    opener = text[index]
    closer = ")" if opener == "(" else "}"
    depth, i, instr, esc = 0, index, False, False
    while i < len(text):
        ch = text[i]
        if instr:
            if esc:
                esc = False
            elif ch == "\\":
                esc = True
            elif ch == '"':
                instr = False
        elif ch == '"':
            instr = True
        elif ch == opener:
            depth += 1
        elif ch == closer:
            depth -= 1
            if depth == 0:
                return text[index + 1:i], i + 1
        i += 1
    raise ValueError("unbalanced %s at %d" % (opener, index))


def split_top_level(text):
    parts, depth, cur, instr, esc = [], 0, "", False, False
    for ch in text:
        if instr:
            cur += ch
            if esc:
                esc = False
            elif ch == "\\":
                esc = True
            elif ch == '"':
                instr = False
        elif ch == '"':
            instr = True
            cur += ch
        elif ch in "([{":
            depth += 1
            cur += ch
        elif ch in ")]}":
            depth -= 1
            cur += ch
        elif ch == "," and depth == 0:
            parts.append(cur)
            cur = ""
        else:
            cur += ch
    parts.append(cur)
    return parts


def parse_initializer(body):
    out = {}
    for part in split_top_level(body):
        part = part.strip()
        if not part or "=" not in part:
            continue
        key, value = part.split("=", 1)
        out[key.strip()] = value.strip()
    return out


def unquote(value):
    value = value.strip()
    if len(value) >= 2 and value[0] == '"' and value[-1] == '"':
        value = value[1:-1]
    return value.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")


def param_type(expression):
    match = re.search(r"Param_\w+", expression)
    return match.group(0) if match else None


def parse_params(block):
    """Every `result.Add(new GH_SAMParam(...))` in a declaration block, in source order."""
    variables = {}
    for match in re.finditer(r"(\w+)\s*=\s*new\s+[\w:.]*Param_\w+\(\)\s*\{", block):
        body, _ = take_balanced(block, block.index("{", match.start()))
        variables[match.group(1)] = {"init": parse_initializer(body), "type": param_type(block[match.start():match.end()])}
    for match in re.finditer(r"(\w+)\.SetPersistentData\(([^;]*)\)", block):
        if match.group(1) in variables:
            variables[match.group(1)]["persistentDefault"] = match.group(2).strip()

    result = []
    for match in re.finditer(r"result\.Add\(new GH_SAMParam\(", block):
        args, _ = take_balanced(block, match.end() - 1)
        parts = split_top_level(args)
        first = parts[0].strip()
        vis = parts[1].strip().replace("ParamVisibility.", "") if len(parts) > 1 else None

        if first.startswith("new "):
            body, _ = take_balanced(block, block.index("{", match.end()))
            init = parse_initializer(body)
            ptype = param_type(first[:first.index("{")])
            persistent = None
        else:
            entry = variables.get(first, {})
            init = entry.get("init", {})
            ptype = entry.get("type")
            persistent = entry.get("persistentDefault")

        result.append({
            "name": unquote(init.get("Name", "")),
            "nickname": unquote(init.get("NickName", "")),
            "description": unquote(init.get("Description", "")),
            "type": ptype,
            "optional": init.get("Optional", "false").strip() == "true",
            "access": (init.get("Access", "") or "").split(".")[-1] or None,
            "visibility": vis,
            "persistentDefault": persistent,
        })
    return result


def is_parameter_class(class_name, base_types):
    """True for a Goo parameter wrapper, not a published component.

    A Grasshopper definition object is a *component* only when it is a `GH_Component`. The
    `GooXxxParam` classes declare a `ComponentGuid` too (they are serialisable document objects), so
    a scanner keyed on `ComponentGuid` alone collects them as well: in the SAM repository that is
    ~60 extra objects whose "parameters" are meaningless. They are recognised by their name (every
    SAM `…Param` class is a parameter) and by deriving from `GH_Param`/`GH_PersistentParam`.
    """
    base = (base_types or "").strip()
    if class_name.endswith("Param") and "Param" in base:
        return True
    return "GH_PersistentParam" in base or "GH_Param" in base


def components(repo):
    """(components in a stable order, skipped parameter-class identifiers) for `repo`."""
    items = []
    skipped = []
    for project_dir, project in sorted(gh_projects(repo).items(), key=lambda kv: kv[1]):
        removed = compile_removed(project_dir)
        for root, dirs, files in os.walk(project_dir):
            dirs[:] = sorted(d for d in dirs if d not in SKIP_DIRS)
            for f in sorted(files):
                if not f.endswith(".cs") or f.endswith(".Designer.cs"):
                    continue
                path = os.path.join(root, f)
                if removed(os.path.relpath(path, project_dir)):
                    continue
                source = open(long_path(path), encoding="utf-8-sig").read()
                if "ComponentGuid" not in source:
                    continue
                code = re.sub(r"(?m)(^|[\s;{}])//[^\n]*", r"\1", source)
                classes = list(CLASS_RE.finditer(code))
                for i, class_match in enumerate(classes):
                    body = code[class_match.end(): classes[i + 1].start() if i + 1 < len(classes) else len(code)]
                    guid = GUID_RE.search(body)
                    if not guid or "abstract" in class_match.group(1):
                        continue
                    if is_parameter_class(class_match.group(2), class_match.group(3)):
                        skipped.append("%s (%s)" % (class_match.group(2), os.path.relpath(path, repo).replace("\\", "/")))
                        continue
                    ctor = re.search(r"\b" + class_match.group(2) + r"\s*\(\s*\)\s*(?=:\s*base\()", body)
                    base = base_args(body, ctor.end()) if ctor else None
                    if base and base[-1].startswith("GH_ParamAccess"):
                        base = base[:-1]

                    version = re.search(r"LatestComponentVersion\s*=>\s*\"([^\"]+)\"", body)
                    inputs_match = re.search(r"GH_SAMParam\[\]\s+Inputs", body)
                    outputs_match = re.search(r"GH_SAMParam\[\]\s+Outputs", body)
                    inputs = parse_params(body[inputs_match.start():outputs_match.start()]) if inputs_match and outputs_match else []
                    outputs = parse_params(body[outputs_match.start():]) if outputs_match else []

                    items.append({
                        "class": class_match.group(2),
                        "guid": guid.group(1).lower(),
                        "name": unquote(lit(base[0])) if base else None,
                        "nickname": unquote(lit(base[1])) if base and len(base) > 1 else None,
                        "category": unquote(lit(base[-2])) if base and len(base) >= 4 else None,
                        "subcategory": unquote(lit(base[-1])) if base and len(base) >= 4 else None,
                        "description": unquote(lit(base[2])) if base and len(base) >= 4 else None,
                        "latestComponentVersion": version.group(1) if version else None,
                        "inputs": inputs,
                        "outputs": outputs,
                        "source": os.path.relpath(path, repo).replace("\\", "/"),
                    })
    return sorted(items, key=lambda item: item["class"]), sorted(skipped)


# --- naming standard and its frozen legacy exceptions ------------------------------------------------

def base_name(name):
    return name.strip("_")


def expected_name(name, side, optional, persistent_default):
    core = base_name(name)
    if side == "output":
        return core[:1].upper() + core[1:]
    if not optional:
        return "_" + core
    return ("_" + core + "_") if persistent_default is not None else (core + "_")


def deviations(contract):
    """Client identifiers whose declared name does not follow the SAM-BIM naming standard."""
    result = []
    for component in contract["components"]:
        for side in ("inputs", "outputs"):
            for param in component[side]:
                expected = expected_name(param["name"], side[:-1], param["optional"], param["persistentDefault"])
                if param["name"] != expected:
                    result.append("%s.%s.%s" % (component["class"], side[:-1], param["name"]))
    return result


def quality_problems(contract):
    problems = []
    for component in contract["components"]:
        if not component["latestComponentVersion"]:
            problems.append("%s has no LatestComponentVersion" % component["class"])
        for side in ("inputs", "outputs"):
            for param in component[side]:
                where = "%s.%s.%s" % (component["class"], side[:-1], param["name"])
                description = (param["description"] or "").strip()
                if not description:
                    problems.append("%s: empty description" % where)
                elif not description[0].isupper():
                    problems.append("%s: description does not begin with a capital letter" % where)
                if "Humidty" in description:
                    problems.append("%s: known typo 'Humidty'" % where)
                if param["nickname"] != param["name"]:
                    problems.append("%s: NickName '%s' != Name" % (where, param["nickname"]))
                if param["access"] != "item":
                    problems.append("%s: unexpected access '%s'" % (where, param["access"]))
    return problems


# --- entry point ------------------------------------------------------------------------------------

def diff(committed, current):
    problems = []
    by_class = {c["class"]: c for c in committed["components"]}
    for component in current["components"]:
        previous = by_class.pop(component["class"], None)
        if previous is None:
            problems.append("new component %s (%s)" % (component["class"], component["guid"]))
            continue
        for field in ("guid", "name", "nickname", "category", "subcategory", "latestComponentVersion"):
            if previous[field] != component[field]:
                problems.append("%s.%s: %r -> %r" % (component["class"], field, previous[field], component[field]))
        for side in ("inputs", "outputs"):
            old = [(p["name"], p["nickname"], p["description"], p["optional"], p["access"], p["visibility"], p["persistentDefault"]) for p in previous[side]]
            new = [(p["name"], p["nickname"], p["description"], p["optional"], p["access"], p["visibility"], p["persistentDefault"]) for p in component[side]]
            if [x[0] for x in old] != [x[0] for x in new]:
                problems.append("%s.%s: names/order changed\n    committed: %s\n    current:   %s"
                                % (component["class"], side, [x[0] for x in old], [x[0] for x in new]))
                continue
            for a, b in zip(old, new):
                # name, nickname, optional, access, visibility and persistent default are frozen contract
                for index, label in ((1, "nickname"), (3, "optional"), (4, "access"), (5, "visibility"), (6, "persistentDefault")):
                    if a[index] != b[index]:
                        problems.append("%s.%s.%s: %s %r -> %r" % (component["class"], side, a[0], label, a[index], b[index]))
    for leftover in by_class:
        problems.append("component removed: %s" % leftover)
    return problems


def manifest_path(repo):
    """The committed manifest for `repo`."""
    return os.path.join(repo, "design", "grasshopper-icons", "param_contract.json")


def main(argv):
    here = os.path.dirname(os.path.abspath(__file__))
    default_repo = os.path.dirname(os.path.dirname(os.path.dirname(here)))

    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--repo", default=default_repo, help="repository to scan (default: the SAM repository this file lives in)")
    parser.add_argument("--output", default=None, help="manifest to write (default: <repo>/design/grasshopper-icons/param_contract.json)")
    parser.add_argument("--check", action="store_true", help="verify source against the committed manifest instead of writing it")
    args = parser.parse_args(argv[1:])

    repo = os.path.abspath(args.repo)
    if not os.path.isdir(repo):
        print("FAIL no such repository: %s" % repo)
        return 2

    found, skipped = components(repo)
    current = {
        "repository": os.path.basename(repo),
        "components": found,
        "namingExceptions": sorted(deviations({"components": found})),
    }

    if args.check:
        path = manifest_path(repo)
        if not os.path.isfile(path):
            print("FAIL no committed manifest at %s" % path)
            return 2
        committed = json.load(open(path, encoding="utf-8"))
        problems = diff(committed, current)
        # naming standard: expected deviations are the documented legacy exceptions, nothing more
        expected_exceptions = sorted(committed.get("namingExceptions", []))
        actual_exceptions = sorted(deviations(current))
        if expected_exceptions != actual_exceptions:
            problems.append("naming exceptions changed (a new deviation, or a stale exception):\n"
                            "    documented: %s\n    actual:     %s" % (expected_exceptions, actual_exceptions))
        problems += quality_problems(current)
        for problem in problems:
            print("FAIL", problem)
        print("%d components checked (%d parameter classes skipped): %s"
              % (len(found), len(skipped), "OK" if not problems else "%d PROBLEM(S)" % len(problems)))
        return 1 if problems else 0

    output = os.path.abspath(args.output) if args.output else manifest_path(repo)
    directory = os.path.dirname(output)
    if directory and not os.path.isdir(directory):
        os.makedirs(directory)
    with open(output, "w", encoding="utf-8", newline="\n") as stream:
        json.dump(current, stream, indent=1)
        stream.write("\n")
    print("%d components (%d parameter classes skipped) -> %s" % (len(found), len(skipped), output))
    print("%d documented naming exceptions" % len(current["namingExceptions"]))
    print("%d description/version problems" % len(quality_problems(current)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
