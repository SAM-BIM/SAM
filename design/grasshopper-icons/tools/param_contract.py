"""The SAM-BIM Grasshopper component metadata contract - canonical shared validator.

Extracts the published parameter contract of every Grasshopper component that declares a
`ComponentGuid`, and freezes it in a manifest (`param_contract.json`) that a later `--check` compares
against the source.

    python param_contract.py --repo <path>                      # refresh the manifest (inventory)
    python param_contract.py --repo <path> --check               # compatibility gate vs the committed manifest
    python param_contract.py --repo <path> --output <file>       # write elsewhere (dry run, no baseline)
    python param_contract.py --repo <path> --check --json        # machine-readable summary

`--repo` defaults to the SAM repository this file lives in; `--output` defaults to
`<repo>/design/grasshopper-icons/param_contract.json`. One shared copy serves every SAM-BIM
repository: the manifest is repository-specific data, the validator is not.

## The governing rule

**Never report a component or repository as verified when the published contract was not completely
and reliably extracted.** Every component in the manifest carries an `extraction.status`:

| status | meaning |
|---|---|
| `verified` | every parameter was extracted: name, runtime type, access, optionality, visibility, registered default |
| `partial` | extracted, but at least one attribute could not be established (recorded in `extraction.reasons`) |
| `unresolved` | the parameter set itself could not be established |

`--check` fails (nonzero) if any component is not `verified`, and never prints `OK` for an incomplete
contract. A baseline-free run is an **inventory**, labelled as such; it is not compatibility
validation and does not claim to be.

The manifest is a *different thing* from the standard's naming conventions:

* **compatibility** (fatal under `--check`) - component identity and the published parameter contract:
  GUID, name, nickname, category, subcategory, and per parameter name, **runtime type**, order, access,
  optionality, visibility and registered default. These are frozen.
* **conventions** (fatal under `--check`) - a naming-rule deviation that is not in the baseline's
  approved `namingExceptions` list, and a listed exception that is no longer a deviation. Existing
  published names are never forced to change; a *new* deviation needs an approved exception.
* **advisory** (non-fatal) - description quality, and other observations that do not change the
  contract. Not labelled as enforced anywhere.

## Schema

Manifests written by this tool are `schemaVersion: 2`. A manifest without `schemaVersion` is the
earlier v1 form (produced by the SAM_Psychrometrics pilot): it is still read, as a baseline whose
parameters carry a runtime type, but it has no `extraction` section - so it is treated as a legacy
baseline and only a fully extracted *current* component may be compared against it.

## Why the scanner is embedded

The C# scanner below is the Grasshopper document-object scanner from SAM#166, in the generalised form
first written for the `SAM_Psychrometrics` metadata pilot, extended here with source-based ancestry
classification and a statement-level parameter extractor. It is inlined rather than imported because
the repository-local `inventory.py` copies have diverged (`SAM/design/grasshopper-icons/tools/
inventory.py` is the older, SAM-only generation with a different API). Consolidating the copies is a
recorded follow-up in `docs/GH_COMPONENT_STANDARDS.md`; this file is the canonical copy and must not be
modified per repository.

See `docs/GH_COMPONENT_STANDARDS.md` for the standard this file enforces.
"""
import argparse
import fnmatch
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

SCHEMA_VERSION = 2

# --- C# scanner -------------------------------------------------------------------------------------

SKIP_DIRS = {"obj", "bin", ".git", "packages", "design", "build", ".vs", "TestResults"}
CLASS_RE = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*((?:public|internal|private|protected|abstract|sealed|static|partial|\s)+)class\s+(\w+)(?:<[^>{]*>)?\s*(?::\s*([^{\n]+))?", re.M)
GUID_RE = re.compile(r"ComponentGuid\s*(?:=>|\{\s*get\s*\{\s*return)\s*new\s*(?:[\w.]*\.)?(?:Guid\s*)?\(\s*\"([0-9a-fA-F-]+)\"")
VERSION_RE = re.compile(r"LatestComponentVersion\s*=>\s*\"([^\"]+)\"")
COMMENT_RE = re.compile(r"(?m)(^|[\s;{}])//[^\n]*")
BLOCK_COMMENT_RE = re.compile(r"/\*.*?\*/", re.S)

# External base types whose ancestry resolves to "a published component".
COMPONENT_ROOTS = {
    "GH_Component",
    "GH_SAMComponent",
    "GH_SAMVariableOutputParameterComponent",
    "GH_SAMAsyncComponent",
    "GH_SAMEnumComponent",
    "IGH_Component",
    # SAM's own component contract. A component whose base lives in another assembly (for example
    # SAM_Revit's `SAMTransactionalChainComponent : RhinoInside.Revit.GH.Components.
    # TransactionalChainComponent, IGH_SAMComponent`) is still a published component.
    "IGH_SAMComponent",
}

# External base types whose ancestry resolves to "a Grasshopper parameter", not a component.
PARAMETER_ROOTS = {
    "GH_Param",
    "GH_PersistentParam",
    "IGH_Param",
}

# `GH_OutputParamManager.Add<X>Parameter(...)` helper -> declared runtime parameter type.
MANAGER_HELPER_TYPES = {
    "Number": "Param_Number",
    "Integer": "Param_Integer",
    "Boolean": "Param_Boolean",
    "Text": "Param_String",
    "String": "Param_String",
    "Point": "Param_Point",
    "Vector": "Param_Vector",
    "Plane": "Param_Plane",
    "Line": "Param_Line",
    "Circle": "Param_Circle",
    "Arc": "Param_Arc",
    "Curve": "Param_Curve",
    "Surface": "Param_Surface",
    "Brep": "Param_Brep",
    "Mesh": "Param_Mesh",
    "Geometry": "Param_Geometry",
    "Generic": "Param_GenericObject",
    "Interval": "Param_Interval",
    "Colour": "Param_Colour",
    "Color": "Param_Colour",
    "Time": "Param_Time",
    "Date": "Param_Time",
    "FilePath": "Param_FilePath",
    "Transform": "Param_Transform",
    "SubD": "Param_SubD",
    "Field": "Param_Field",
    "Group": "Param_Group",
    "ComplexNumber": "Param_Complex",
    "Culture": "Param_Culture",
    "Guid": "Param_Guid",
}

ACCESS_VALUES = {"item", "list", "tree"}

# A parameter object assigned to a variable: `Type v = new ...` or `v = new ...`.
ASSIGNMENT_RE = re.compile(
    r"(?:^|[;{}()\s])(?:(?P<type>[A-Za-z_][\w:.<>,\[\]]*)\s+)?(?P<name>" + r"@?[A-Za-z_]\w*" + r")\s*=\s*(?P<rhs>new\b[^;]*)")

# A C# identifier, including a verbatim identifier (`@string`), which SAM components use for
# parameter variables whose name collides with a keyword.
IDENT = r"@?[A-Za-z_]\w*"


def identifier(name):
    """`@string` and `string` are the same C# identifier; the `@` is not part of the name."""
    return name[1:] if name.startswith("@") else name


def long_path(path):
    """A \\\\?\\ path on Windows, so a path over MAX_PATH can still be read."""
    path = os.path.abspath(path)
    return "\\\\?\\" + path if os.name == "nt" and not path.startswith("\\\\") else path


def read_text(path):
    with open(long_path(path), encoding="utf-8-sig", errors="replace") as stream:
        return stream.read()


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


# --- C# lexing helpers -------------------------------------------------------------------------------

def take_balanced(text, index):
    """Text between text[index] ('(' / '{' / '[') and its matching closer, and the index after it."""
    pairs = {"(": ")", "{": "}", "[": "]"}
    opener = text[index]
    closer = pairs[opener]
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


def split_top_level(text, separator=","):
    """Split on `separator` at depth 0, respecting strings and nesting."""
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
        elif ch == separator and depth == 0:
            parts.append(cur)
            cur = ""
        else:
            cur += ch
    parts.append(cur)
    return parts


def split_statements(body):
    """Top-level `;`-separated statements of a C# block, strings and nesting respected."""
    statements, cur, depth, instr, esc = [], "", 0, False, False
    for ch in body:
        if instr:
            cur += ch
            if esc:
                esc = False
            elif ch == "\\":
                esc = True
            elif ch == '"':
                instr = False
            continue
        if ch == '"':
            instr = True
            cur += ch
            continue
        if ch in "([{":
            depth += 1
        elif ch in ")]}":
            depth -= 1
        elif ch == ";" and depth == 0:
            statements.append(cur)
            cur = ""
            continue
        cur += ch
    if cur.strip():
        statements.append(cur)
    return statements


# Statement-block headers whose `{ ... }` body is a nested statement sequence rather than an object or
# collection initializer. Parameter declarations inside such a block (a `foreach` over enum values, an
# `if` guarding a voluntary parameter) are real published parameters and must be read.
BLOCK_HEADERS = ("foreach", "for", "if", "else", "while", "do", "switch", "using", "try", "catch",
                 "finally", "lock", "unsafe", "fixed", "checked", "unchecked")


def is_block_header(text):
    """Whether the text before a `{` is a statement-block header rather than an initializer.

    The parentheses must be balanced at the `{`: in
    `foreach (string name in new string[] { "_a" }) { ... }` the first `{` belongs to the array
    initializer inside the loop's own parentheses, not to the loop body, and descending into it would
    hide the loop body's statements.
    """
    header = text.strip()
    depth = 0
    for ch in header:
        if ch in "([":
            depth += 1
        elif ch in ")]":
            depth -= 1
    if depth != 0:
        return False
    if header.endswith("=>"):
        return True
    for keyword in BLOCK_HEADERS:
        if header == keyword or header.startswith(keyword + " ") or header.startswith(keyword + "("):
            return True
    return False


def walk_statements(body):
    """Statements in source order, descending into nested statement blocks.

    `split_statements` stops at a nested `{ ... }`, which hides every parameter declared inside a
    `foreach` or an `if` in the property body. Object and collection initializers are *not* descended
    into - they are part of the statement they belong to - so the distinction is the block header.
    """
    result, current, i = [], "", 0
    while i < len(body):
        ch = body[i]
        if ch == '"':
            current += ch
            i += 1
            while i < len(body):
                current += body[i]
                if body[i] == "\\":
                    i += 1
                    if i < len(body):
                        current += body[i]
                elif body[i] == '"':
                    break
                i += 1
            i += 1
            continue
        if ch == "{":
            inner, after = take_balanced(body, i)
            if is_block_header(current):
                result.extend(walk_statements(inner))
                current = ""
            else:
                current += "{" + inner + "}"
            i = after
            continue
        if ch == ";":
            if current.strip():
                result.append(current)
            current = ""
            i += 1
            continue
        current += ch
        i += 1
    if current.strip():
        result.append(current)
    return result


def split_assignment(statement):
    """(lhs, rhs) for a single top-level assignment, else None. Ignores `==`, `=>`, `+=`, ..."""
    depth, instr, esc, i = 0, False, False, 0
    while i < len(statement):
        ch = statement[i]
        if instr:
            if esc:
                esc = False
            elif ch == "\\":
                esc = True
            elif ch == '"':
                instr = False
            i += 1
            continue
        if ch == '"':
            instr = True
            i += 1
            continue
        if ch in "([{":
            depth += 1
        elif ch in ")]}":
            depth -= 1
        elif ch == "=" and depth == 0:
            nxt = statement[i + 1:i + 2]
            prv = statement[i - 1:i]
            if nxt != "=" and nxt != ">" and prv not in ("=", "!", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^"):
                return statement[:i], statement[i + 1:]
        i += 1
    return None


def assignment_target(lhs):
    """(declared type text or None, variable name) for an assignment target, else None.

    `a.b = x` is not a variable assignment and returns None; `Type v = x` and `v = x` do.
    """
    lhs = lhs.strip()
    match = re.fullmatch(r"(?:(?P<type>[A-Za-z_][\w:.<>,\[\]\s]*?)\s+)?(?P<name>" + IDENT + r")", lhs)
    if not match:
        return None
    return match.group("type"), identifier(match.group("name"))


def simple_type(type_text):
    """`global::A.B.Param_Brep` -> `Param_Brep`; `GooX<Y>` -> `GooX`."""
    if not type_text:
        return None
    text = type_text.strip()
    text = text.split("<")[0]
    text = text.split("::")[-1]
    text = text.split(".")[-1].strip()
    return text or None


def literal_string(expression):
    """(text, True) for a string literal or a `+`-concatenation of literals, else (raw, False)."""
    expression = expression.strip()
    if not expression:
        return None, False
    parts = split_top_level(expression, "+")
    pieces = []
    for part in parts:
        part = part.strip()
        if re.fullmatch(r'"(?:[^"\\]|\\.)*"', part):
            pieces.append(part[1:-1].replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\"))
        else:
            return expression, False
    return "".join(pieces), True


def parse_initializer(body):
    """`{ Name = "x", Access = GH_ParamAccess.item }` -> {Name: 'x', ...} (raw right-hand sides)."""
    out = {}
    for part in split_top_level(body):
        part = part.strip()
        if not part:
            continue
        assignment = split_assignment(part)
        if not assignment:
            continue
        target = assignment_target(assignment[0])
        if not target:
            continue
        out[target[1]] = assignment[1].strip()
    return out


def parse_new_expression(expression, declared_type=None):
    """(type name or None, initializer dict or None) for a `new` expression, else None.

    Handles `new T()`, `new T() { ... }`, `new T { ... }` (object initializer with no constructor
    parentheses - a very common SAM form), `new A.B.T<X>() { ... }`, and target-typed `new ()`/`new()`
    whose type comes from the declared type.
    """
    expression = expression.strip()
    if not expression.startswith("new"):
        return None
    rest = expression[3:].lstrip()
    if rest.startswith("("):                      # target-typed `new (...)` / `new()`
        body, after = take_balanced(rest, 0)
        type_name = simple_type(declared_type)
    else:
        match = re.match(r"([A-Za-z_][\w:.<>,\[\]]*)\s*(?:\(|\{)", rest)
        if not match:
            return None
        type_name = simple_type(match.group(1))
        if rest[match.end() - 1] == "(":
            body, after = take_balanced(rest, match.end() - 1)
        else:
            body, after = "", match.end() - 1   # `new T { ... }`: initializer only
    tail = rest[after:]
    initializer = None
    brace = tail.find("{")
    if brace != -1 and tail[:brace].strip() == "":
        brace_body, _ = take_balanced(tail, brace)
        initializer = parse_initializer(brace_body)
    return type_name, initializer


def access_of(raw):
    """`GH_ParamAccess.item` -> `item`."""
    if raw is None:
        return None
    value = raw.strip()
    if not value:
        return None
    return value.split(".")[-1].strip()


def visibility_of(raw):
    if raw is None:
        return None
    value = raw.strip()
    if value.startswith("ParamVisibility."):
        value = value[len("ParamVisibility."):]
    return value or None


def create_diagnostic_record(code, message, severity="warning"):
    return {"code": code, "message": message, "severity": severity}


def type_kind(type_name):
    """`builtin` (Grasshopper `Param_*`), `goo` (a SAM `...Param` class), `external` (any other
    constructed parameter type, e.g. RhinoInside.Revit's `HostObject`), or None when the type text is
    not a usable type name at all.
    """
    if not type_name:
        return None
    if re.fullmatch(r"Param_\w+", type_name):
        return "builtin"
    if re.fullmatch(r"\w*Param", type_name):
        return "goo"
    if re.fullmatch(r"[A-Za-z_]\w*", type_name):
        return "external"
    return None


def is_param_type_name(type_name):
    """A recognisable Grasshopper parameter type name: built-in `Param_X` or SAM `XParam`."""
    return type_kind(type_name) in ("builtin", "goo")


FACTORY_RE = re.compile(
    r"(?:public|internal|private|protected)?\s*static\s+"
    r"(?:global::[\w.]*\.)?(?P<type>Param_\w+|\w+Param)\s+(?P<name>[A-Za-z_]\w*)\s*\((?P<args>[^)]*)\)\s*\{", re.S)


def substitute_identifiers(text, mapping):
    """Replace whole identifiers in `text` with the mapped expression."""
    for name, value in mapping.items():
        text = re.sub(r"(?<![\w.])" + re.escape(name) + r"\b", lambda _match, v=value: v, text)
    return text


def parse_factories(body):
    """{method: decider} for local parameter factories.

    A SAM component commonly builds its parameters through a private static helper:

        private static Param_Number Number(string name, string description, double defaultValue)
        {
            Param_Number result = new Param_Number() { Name = name, NickName = name, ... };
            result.SetPersistentData(defaultValue);
            return result;
        }

    Only a method whose **declared return type is a parameter type** and whose body constructs exactly
    one parameter object is treated as a factory, so an unrelated overload (`int Integer(IGH_DataAccess,
    string)`) is never mistaken for one. Anything else stays unsupported and is reported.
    """
    factories = {}
    for match in FACTORY_RE.finditer(body):
        brace = body.index("{", match.end() - 1)
        method_body, _ = take_balanced(body, brace)
        constructions = list(re.finditer(r"new\s+([A-Za-z_][\w:.<>,\[\]]*)\s*(?:\(|\{)", method_body))
        if len(constructions) != 1:
            continue
        constructed = constructions[0]
        type_name = simple_type(constructed.group(1))
        if type_name is None:
            continue
        if method_body[constructed.end() - 1] == "(":
            _, after = take_balanced(method_body, constructed.end() - 1)
        else:
            after = constructed.end() - 1
        tail = method_body[after:]
        brace_at = tail.find("{")
        if brace_at == -1 or tail[:brace_at].strip() != "":
            continue
        initializer_body, _ = take_balanced(tail, brace_at)
        initializer = parse_initializer(initializer_body)
        default = re.search(r"\.\s*SetPersistentData\s*\(", method_body)
        default_expression = None
        if default:
            default_expression, _ = take_balanced(method_body, default.end() - 1)
        formal = []
        for part in split_top_level(match.group("args")):
            part = part.strip()
            if not part:
                continue
            tokens = part.split()
            if len(tokens) < 2:
                formal = []
                break
            formal.append(identifier(tokens[-1]))
        if not formal:
            continue
        factories[match.group("name")] = {
            "type": simple_type(type_name) or type_name,
            "formal": formal,
            "init": initializer,
            "default": default_expression.strip() if default_expression else None,
        }
    return factories


# --- project discovery --------------------------------------------------------------------------------

def xml_local(tag):
    return tag.rsplit("}", 1)[-1] if "}" in tag else tag


def parse_project(path):
    """(root Element, error message). ElementTree handles both `"` and `'` attribute quoting."""
    try:
        return ET.parse(long_path(path)).getroot(), None
    except ET.ParseError as error:
        return None, "csproj could not be parsed as XML: %s" % error
    except OSError as error:
        return None, "csproj could not be read: %s" % error


def project_references(root):
    """(direct evidence strings, referenced assembly simple names, project reference paths)."""
    direct, assemblies, projects = [], [], []
    for element in root.iter():
        name = xml_local(element.tag)
        if name == "PackageReference" or name == "Reference":
            include = element.get("Include") or element.get("Update") or ""
            if include:
                simple = include.split(",")[0].strip()
                assemblies.append(simple)
                if "grasshopper" in simple.lower() or simple.lower() in ("gh_io", "gh_util"):
                    direct.append("%s Include=%s" % (name, simple))
        elif name == "HintPath":
            value = (element.text or "").strip()
            if value:
                simple = os.path.basename(value)
                assemblies.append(simple)
                if "grasshopper" in value.lower():
                    direct.append("HintPath=%s" % value)
        elif name == "ProjectReference":
            include = element.get("Include") or ""
            if include:
                projects.append(include.replace("\\", "/"))
    return direct, assemblies, projects


def compile_removed(root):
    """Predicate for files excluded from compilation by the project's `<Compile Remove="..."/>` globs."""
    patterns = []
    for element in root.iter():
        if xml_local(element.tag) == "Compile":
            pattern = element.get("Remove")
            if pattern:
                patterns.append(pattern.replace("\\", "/"))

    def removed(relative):
        relative = relative.replace("\\", "/")
        return any(fnmatch.fnmatch(relative, pattern) or fnmatch.fnmatch(relative, pattern.replace("**/", ""))
                   for pattern in patterns)

    return removed


def discover_projects(repo):
    """Every .csproj in the repository, with the evidence for its Grasshopper classification.

    Direct: the project itself references Grasshopper (package, assembly or hint path).
    Indirect: it references a project in the repository that is itself a Grasshopper project - resolved
    to a fixed point, so a chain of project references is followed.

    ElementTree parses the XML, so both `"` and `'` attribute quoting work and a project that cannot be
    parsed is reported instead of being silently dropped.
    """
    candidates = {}
    for root, dirs, files in os.walk(repo):
        dirs[:] = sorted(d for d in dirs if d not in SKIP_DIRS)
        for file_name in sorted(files):
            if not file_name.endswith(".csproj"):
                continue
            path = os.path.join(root, file_name)
            element, error = parse_project(path)
            entry = {"name": file_name[:-7], "path": path, "root": element, "error": error,
                     "direct": [], "assemblies": [], "projects": [],
                     "is_test": "Test" in file_name}
            if element is not None:
                entry["direct"], entry["assemblies"], entry["projects"] = project_references(element)
            candidates[path] = entry

    by_path = {os.path.normcase(os.path.normpath(p)): p for p in candidates}
    state = {}
    for path, info in candidates.items():
        if info["error"]:
            continue
        if info["direct"] or any("grasshopper" in assembly.lower() for assembly in info["assemblies"]):
            state[path] = "direct"

    changed = True
    while changed:
        changed = False
        for path, info in candidates.items():
            if path in state or info["error"]:
                continue
            for reference in info["projects"]:
                target = os.path.normcase(os.path.normpath(os.path.join(os.path.dirname(path), reference)))
                if target in by_path and by_path[target] in state:
                    state[path] = "indirect:%s" % candidates[by_path[target]]["name"]
                    changed = True
                    break

    return candidates, state


# --- class classification -----------------------------------------------------------------------------

def declared_bases(base_text):
    """Simple base type names of a class declaration, interfaces included."""
    if not base_text:
        return []
    result = []
    for part in split_top_level(base_text):
        part = part.strip()
        if not part:
            continue
        name = simple_type(part.split("(")[0])
        if name:
            result.append(name)
    return result


def classify_classes(repo, publish_dirs, scan_dirs=None):
    """Every class that declares a ComponentGuid, with its repository ancestry.

    `publish_dirs` are the scanned Grasshopper projects whose classes may become published components;
    `scan_dirs` (default: the same) additionally supplies classes used **only** to resolve ancestry, so
    a base class that lives in a non-Grasshopper project of the same repository still resolves instead
    of leaving its subclasses unclassified. Test projects are never scanned.

    Returns (components, parameter_classes, unclassified, all_classes). Classification is by
    **ancestry**, resolved through the repository's own classes and then against the known Grasshopper
    root types - never by the class name, so a real component called `NewComputeParam` is a component
    and `GooSpaceParam` is a parameter. Abstract classes are kept as resolvable ancestry but are never
    published components. Files excluded by `<Compile Remove="..."/>` are not compiled and are skipped.
    """
    scan_dirs = scan_dirs if scan_dirs is not None else publish_dirs
    publish = {os.path.normcase(os.path.normpath(directory)) for directory, _root in publish_dirs}

    candidates = []
    for project_dir, root_element in scan_dirs:
        removed = compile_removed(root_element) if root_element is not None else (lambda relative: False)
        is_publishable = os.path.normcase(os.path.normpath(project_dir)) in publish
        for root, dirs, files in os.walk(project_dir):
            dirs[:] = sorted(d for d in dirs if d not in SKIP_DIRS)
            for file_name in sorted(files):
                if not file_name.endswith(".cs") or file_name.endswith(".Designer.cs"):
                    continue
                path = os.path.join(root, file_name)
                if removed(os.path.relpath(path, project_dir)):
                    continue
                source = read_text(path)
                if "ComponentGuid" not in source and "abstract class" not in source:
                    continue
                code = BLOCK_COMMENT_RE.sub(" ", COMMENT_RE.sub(r"\1", source))
                matches = list(CLASS_RE.finditer(code))
                for index, match in enumerate(matches):
                    body = code[match.end(): matches[index + 1].start() if index + 1 < len(matches) else len(code)]
                    declaration = match.group(0)
                    generics = re.search(r"class\s+\w+\s*<\s*([^>]*)\s*>", declaration)
                    candidates.append({
                        "class": match.group(2),
                        "modifiers": match.group(1).strip(),
                        "abstract": "abstract" in match.group(1),
                        "publish": is_publishable,
                        "bases": declared_bases(match.group(3)),
                        "base_text": match.group(3) or "",
                        "generics": [identifier(item.strip()) for item in split_top_level(generics.group(1))] if generics else [],
                        "guid": (GUID_RE.search(body).group(1).lower() if GUID_RE.search(body) else None),
                        "body": body,
                        "source": os.path.relpath(path, repo).replace("\\", "/"),
                    })

    bases_by_class = {}
    for candidate in candidates:
        bases_by_class.setdefault(candidate["class"], []).extend(candidate["bases"])

    components, parameters, unclassified = [], [], []
    for candidate in candidates:
        if candidate["abstract"] or not candidate["publish"]:
            continue  # abstract bases and non-Grasshopper projects are ancestry, not published components
        if not candidate["guid"]:
            continue  # a document object without a component identity is not part of the contract
        kind, reason = resolve_kind(candidate["class"], candidate["bases"], bases_by_class)
        candidate["kind"] = kind
        candidate["kind_reason"] = reason
        if kind == "component":
            components.append(candidate)
        elif kind == "parameter":
            parameters.append(candidate)
        else:
            unclassified.append(candidate)
    return components, parameters, unclassified, {candidate["class"]: candidate for candidate in candidates}


def resolve_kind(class_name, bases, bases_by_class, seen=None):
    """('component'|'parameter'|'unresolved', reason) from the base chain, source-based."""
    seen = set(seen or ())
    if class_name in seen:
        return "unresolved", "circular base chain"
    seen.add(class_name)
    if not bases:
        return "unresolved", "no base type declared"

    if any(base in PARAMETER_ROOTS for base in bases):
        return "parameter", "derives from %s" % next(base for base in bases if base in PARAMETER_ROOTS)
    if any(base in COMPONENT_ROOTS for base in bases):
        return "component", "derives from %s" % next(base for base in bases if base in COMPONENT_ROOTS)

    for base in bases:
        if base in bases_by_class:
            kind, reason = resolve_kind(base, bases_by_class[base], bases_by_class, seen)
            if kind != "unresolved":
                return kind, "%s -> %s" % (base, reason)
    return "unresolved", "base chain leaves the repository at unknown type(s): %s" % ", ".join(bases)


def ancestry(class_name, bases_by_class, seen=None):
    """Every base type name reachable from a class, through the repository's own classes."""
    seen = seen if seen is not None else set()
    if class_name in seen:
        return seen
    seen.add(class_name)
    for base in bases_by_class.get(class_name, []):
        seen.add(base)
        if base in bases_by_class:
            ancestry(base, bases_by_class, seen)
    return seen


# --- parameter extraction ------------------------------------------------------------------------------

class Extractor:
    """Extracts a component's declared parameter set from C# source, or explains why it cannot."""

    def __init__(self, class_name, body, substitutions=None):
        self.class_name = class_name
        self.body = body
        self.substitutions = substitutions or {}
        self.reasons = []
        self.external_types = set()
        self._factories = None

    @property
    def factories(self):
        if self._factories is None:
            self._factories = parse_factories(self.body)
        return self._factories

    def reason(self, message):
        if message not in self.reasons:
            self.reasons.append(message)

    # -- helpers -------------------------------------------------------------------------------
    def param(self, name, nickname, description, type_name, optional, access, visibility,
              persistent_default, default_literal, registration, extra=None, position=None):
        record = {
            "name": name,
            "nickname": nickname,
            "description": description,
            "type": type_name,
            "typeKind": type_kind(type_name),
            "optional": optional,
            "access": access,
            "visibility": visibility,
            "persistentDefault": persistent_default,
            "defaultLiteral": default_literal,
            "registration": registration,
        }
        if position is not None:
            record["declaration"] = position
        if extra:
            record.update(extra)
        return record

    def bind(self, bindings, statement, position):
        """Record `Type v = <new expression>`, `v = <new expression>` and `Type v;` bindings."""
        assignment = split_assignment(statement)
        if not assignment:
            bare = re.fullmatch(r"\s*(?P<type>[A-Za-z_][\w:.<>,\[\]]*)\s+(?P<name>" + IDENT + r")\s*", statement)
            if bare:
                # `Param_String param_String;` followed by `param_String = new() { ... }`: the type is
                # declared here and the initializer comes later, so record the declaration's type.
                bindings[identifier(bare.group("name"))] = {"type": simple_type(bare.group("type")), "init": {},
                                                            "default": None, "defaultLiteral": None, "at": position}
            return
        target = assignment_target(assignment[0])
        if not target:
            return
        declared_type, name = target
        rhs = assignment[1].strip()
        if rhs.startswith("new"):
            parsed = parse_new_expression(rhs, declared_type)
            if parsed:
                type_name, initializer = parsed
                previous = bindings.get(name)
                if type_name is None and previous is not None:
                    # `v = new() { ... }` re-uses the variable's declared type; without this the type of
                    # every reassigned parameter variable (a very common SAM pattern) is lost.
                    type_name = previous.get("type")
                bindings[name] = {"type": type_name, "init": initializer or {}, "default": None,
                                  "defaultLiteral": None, "at": position}
        elif rhs.startswith("["):
            bindings.pop(name, None)

    def param_from_binding(self, binding, position):
        if binding is None:
            return None
        initializer = binding["init"]
        return self.param(
            name=self.literal(initializer.get("Name"), position, "Name"),
            nickname=self.literal(initializer.get("NickName"), position, "NickName"),
            description=self.literal(initializer.get("Description"), position, "Description", required=False),
            type_name=binding["type"],
            optional=self.optional(initializer, position),
            access=self.access(initializer.get("Access"), position),
            visibility=None,
            persistent_default=self.default_text(binding),
            default_literal=bool(binding["defaultLiteral"]) if binding["default"] is not None else None,
            registration="declarative",
            position=position,
        )

    def literal(self, raw, position, field, required=True):
        if raw is None:
            if required:
                self.reason("parameter %s not declared at %s" % (field, position))
            return None
        for parameter, argument in self.substitutions.items():
            # `typeof(T).Name` in a generic base resolves to the closed type argument the subclass
            # supplies (`class X : GH_SAMEnumComponent<ApertureType>` publishes the output
            # "ApertureType"). Only a type parameter the subclass actually closes is substituted.
            raw = raw.replace("typeof(%s).Name" % parameter, '"%s"' % argument)
        text, is_literal = literal_string(raw)
        if not is_literal:
            if required:
                self.reason("parameter %s is not a string literal at %s (%s)" % (field, position, raw.strip()[:60]))
            return text
        return text

    def boolean(self, raw, position, field):
        if raw is None:
            return None
        value = raw.strip().lower()
        if value == "true":
            return True
        if value == "false":
            return False
        self.reason("parameter %s is not a literal boolean at %s (%s)" % (field, position, raw.strip()[:40]))
        return None

    def optional(self, initializer, position):
        """Grasshopper's `Optional` is false unless the declaration sets it; an unreadable value is not.

        This is a real, determinable default, so an absent `Optional` must not be reported as
        unresolved - only an explicitly non-literal one is.
        """
        if "Optional" not in (initializer or {}):
            return False
        return self.boolean(initializer.get("Optional"), position, "Optional")

    def access(self, raw, position):
        """Grasshopper's parameter access is `item` unless the declaration sets it."""
        if raw is None or not str(raw).strip():
            return "item"
        value = access_of(raw)
        if value not in ACCESS_VALUES:
            self.reason("unrecognised parameter access '%s' at %s" % (value, position))
            return None
        return value

    def default_text(self, binding):
        if binding["default"] is None:
            return None
        return binding["default"].strip()

    def check_type(self, type_name, position):
        kind = type_kind(type_name)
        if kind is None:
            self.reason("parameter runtime type could not be determined at %s" % position)
        elif kind == "external":
            self.external_types.add(type_name)

    def factory_parameter(self, expression, position):
        """Resolve `Helper("name", "description")` through a local parameter factory, or None.

        The factory's formal parameters are substituted with the call arguments, so the resolved
        parameter carries the same name, nickname, description, access, optionality and registered
        default that a runtime instance would - or, if the substitution does not produce literals, it
        is reported rather than guessed.
        """
        match = re.fullmatch(r"(?P<name>[A-Za-z_]\w*)\s*\((?P<args>.*)\)\s*", expression.strip(), re.S)
        if not match:
            return None
        factory = self.factories.get(match.group("name"))
        if factory is None:
            return None
        arguments = [part.strip() for part in split_top_level(match.group("args"))]
        if len(arguments) != len(factory["formal"]):
            self.reason("parameter factory %s is called with %d argument(s) at %s, not its %d parameter(s)"
                        % (match.group("name"), len(arguments), position, len(factory["formal"])))
            return None
        mapping = dict(zip(factory["formal"], arguments))
        initializer = {key: substitute_identifiers(value, mapping) for key, value in factory["init"].items()}
        default = substitute_identifiers(factory["default"], mapping) if factory["default"] else None
        self.check_type(factory["type"], position)
        return self.param(
            name=self.literal(initializer.get("Name"), position, "Name"),
            nickname=self.literal(initializer.get("NickName"), position, "NickName"),
            description=self.literal(initializer.get("Description"), position, "Description", required=False),
            type_name=factory["type"],
            optional=self.optional(initializer, position),
            access=self.access(initializer.get("Access"), position),
            visibility=None,
            persistent_default=default,
            default_literal=(bool(re.fullmatch(r'-?\d+(\.\d+)?[fFdDmM]?|true|false|"(?:[^"\\]|\\.)*"', default))
                             if default else None),
            registration="factory:%s" % match.group("name"),
            position=position,
        )

    # -- declarative `GH_SAMParam[] Inputs / Outputs` -------------------------------------------
    def declarative(self, side, property_body):
        params = []
        bindings = {}
        for statement in walk_statements(property_body):
            if re.search(r"Params\s*\.\s*Register(Input|Output)Param\s*\(", statement):
                self.reason("a parameter is registered directly through Params.Register*Param inside the %s declaration and cannot be attributed" % side)
                continue
            self.bind(bindings, statement, "%s/%s" % (side, self.position_of(property_body, statement)))
            self.apply_persistent_data(bindings, statement)
            params.extend(self.emit(statement, side, property_body, bindings))
        return params

    def position_of(self, body, statement):
        index = body.find(statement)
        return "offset %d" % index if index >= 0 else "unknown offset"

    def apply_persistent_data(self, bindings, statement):
        match = re.match(r"^\s*(" + IDENT + r")\s*\.\s*SetPersistentData\s*\(", statement.strip())
        if not match:
            return
        name = identifier(match.group(1))
        if name not in bindings:
            self.reason("SetPersistentData on unknown variable '%s'" % name)
            return
        args, _ = take_balanced(statement.strip(), match.end() - 1)
        value = args.strip()
        binding = bindings[name]
        binding["default"] = value
        binding["defaultLiteral"] = bool(re.fullmatch(r'-?\d+(\.\d+)?[fFdDmM]?|true|false|"(?:[^"\\]|\\.)*"', value))

    def emit(self, statement, side, body, bindings):
        """Every `new GH_SAMParam(...)` in one statement, in order.

        This is the identity of a published parameter by construction: a parameter is published only
        through a GH_SAMParam, whether it is added to a list, put in a collection expression, returned
        directly or passed to AddRange. Requiring a particular surrounding call is what made the
        earlier extractor miss whole components.
        """
        params = []
        cursor = 0
        while True:
            index = statement.find("new GH_SAMParam(", cursor)
            if index == -1:
                break
            args, after = take_balanced(statement, index + len("new GH_SAMParam"))
            cursor = after
            parts = split_top_level(args)
            first = parts[0].strip() if parts else ""
            visibility = visibility_of(parts[1] if len(parts) > 1 else None)
            position = "%s/%s" % (side, self.position_of(body, statement))
            if not first:
                self.reason("GH_SAMParam with no arguments at %s" % position)
                continue
            if first.startswith("new"):
                parsed = parse_new_expression(first)
                if parsed is None:
                    self.reason("GH_SAMParam argument is not a recognisable parameter expression at %s" % position)
                    continue
                type_name, initializer = parsed
                self.check_type(type_name, position)
                record = self.param(
                    name=self.literal((initializer or {}).get("Name"), position, "Name"),
                    nickname=self.literal((initializer or {}).get("NickName"), position, "NickName"),
                    description=self.literal((initializer or {}).get("Description"), position, "Description", required=False),
                    type_name=type_name,
                    optional=self.optional(initializer or {}, position),
                    access=self.access((initializer or {}).get("Access"), position),
                    visibility=visibility,
                    persistent_default=None,
                    default_literal=None,
                    registration="declarative",
                    position=position,
                )
            else:
                name = identifier(simple_type(first.split(".")[-1]) or first)
                binding = bindings.get(name)
                if binding is not None:
                    self.check_type(binding["type"], position)
                    record = self.param_from_binding(binding, position)
                    record["visibility"] = visibility
                    record["persistentDefault"] = self.default_text(binding)
                    if binding["default"] is not None:
                        record["defaultLiteral"] = bool(binding["defaultLiteral"])
                else:
                    record = self.factory_parameter(first, position)
                    if record is None:
                        if re.fullmatch(IDENT, name):
                            self.reason("GH_SAMParam uses parameter variable '%s' that was not declared at %s" % (name, position))
                        else:
                            self.reason("GH_SAMParam argument at %s is not a parameter construction, a declared variable "
                                        "or a known parameter factory (%s)" % (position, first.strip()[:70]))
                        continue
                    record["visibility"] = visibility
            params.append(record)
        return params

    # -- manager registration `RegisterInputParams` / `RegisterOutputParams` ---------------------
    def manager(self, side, method_body):
        params = []
        for statement in walk_statements(method_body):
            if re.search(r"Params\s*\.\s*(Input|Output)\s*\[", statement):
                self.reason("optionality, access or visibility is set through Params.Input/Output and cannot be attributed (%s)" % side)
            if "Params.RegisterInputParam" in statement or "Params.RegisterOutputParam" in statement:
                self.reason("a parameter is registered directly through Params.Register*Param and cannot be attributed (%s)" % side)
                continue
            params.extend(self.manager_statement(statement, side, method_body))
        return params

    def manager_statement(self, statement, side, body):
        match = re.search(r"\.\s*Add([A-Za-z_]*?)Parameter\s*\(", statement)
        if not match:
            if re.search(r"\.\s*Add\(.*GH_SAMParam", statement):
                self.reason("GH_SAMParam is passed to a manager call that is not AddParameter (%s)" % side)
            return []
        helper = match.group(1)
        args, _ = take_balanced(statement, match.end() - 1)
        parts = [part.strip() for part in split_top_level(args)]
        position = "%s/%s" % (side, self.position_of(body, statement))
        variable_optional = None

        if helper == "":
            if not parts:
                self.reason("AddParameter with no arguments at %s" % position)
                return []
            first = parts[0]
            if first.startswith("new"):
                parsed = parse_new_expression(first)
                if parsed is None:
                    self.reason("AddParameter argument is not a recognisable parameter expression at %s" % position)
                    return []
                type_name, initializer = parsed
                initializer = initializer or {}
                variable_optional = self.optional(initializer, position)
            elif not re.fullmatch(IDENT, identifier(simple_type(first.split(".")[-1]) or first)):
                record = self.factory_parameter(first, position)
                if record is None:
                    self.reason("AddParameter argument at %s is not a parameter construction, a declared variable "
                                "or a known parameter factory (%s)" % (position, first.strip()[:70]))
                    return []
                return [record]
            else:
                name = simple_type(first.split(".")[-1]) or first
                variable = identifier(name)
                found = self.find_variable(" ".join([first, variable]))
                if found is None:
                    self.reason("AddParameter uses parameter variable '%s' that cannot be resolved unambiguously at %s" % (variable, position))
                    return []
                type_name, initializer = found
                initializer = initializer or {}
                variable_optional = self.variable_optional(variable, position)
                if re.search(r"\b" + re.escape(variable) + r"\s*\.\s*(Name|NickName|Access|Description)\s*=", self.body):
                    self.reason("parameter identity of variable '%s' is set outside the registration call and cannot be attributed (%s)" % (variable, position))
            if len(parts) == 1:
                name_raw, nick_raw, desc_raw, access_raw = (initializer.get("Name"), initializer.get("NickName"),
                                                            initializer.get("Description"), initializer.get("Access"))
            elif len(parts) == 4:
                name_raw, nick_raw, desc_raw, access_raw = parts[1], parts[2], parts[3], initializer.get("Access")
            elif len(parts) >= 5:
                name_raw, nick_raw, desc_raw, access_raw = parts[1], parts[2], parts[3], parts[4]
            else:
                self.reason("unrecognised AddParameter overload with %d arguments at %s" % (len(parts), position))
                return []
        else:
            type_name = MANAGER_HELPER_TYPES.get(helper)
            if type_name is None:
                self.reason("unrecognised parameter manager helper Add%sParameter at %s" % (helper, position))
                return []
            if len(parts) == 3:
                name_raw, nick_raw, desc_raw, access_raw = parts[0], parts[1], parts[2], None
            elif len(parts) == 4:
                name_raw, nick_raw, desc_raw, access_raw = parts[0], parts[1], parts[2], parts[3]
            elif len(parts) == 5:
                name_raw, nick_raw, desc_raw, access_raw = parts[0], parts[1], parts[2], parts[3]
            else:
                self.reason("unrecognised Add%sParameter overload with %d arguments at %s" % (helper, len(parts), position))
                return []
            initializer = {}

        self.check_type(type_name, position)
        persistent = parts[4] if helper != "" and len(parts) >= 5 else None
        if variable_optional is None:
            # Grasshopper's `Optional` is false unless something sets it; an explicit
            # `v.Optional = ...` is read from the parameter object, so an absent one is false.
            variable_optional = False
        record = self.param(
            name=self.literal(name_raw, position, "Name"),
            nickname=self.literal(nick_raw, position, "NickName"),
            description=self.literal(desc_raw, position, "Description", required=False),
            type_name=type_name,
            optional=variable_optional,
            access=self.access(access_raw, position) or "item",
            visibility=None,
            persistent_default=persistent.strip() if persistent else None,
            default_literal=(bool(re.fullmatch(r'-?\d+(\.\d+)?[fFdDmM]?|true|false|"(?:[^"\\]|\\.)*"', persistent.strip()))
                             if persistent else None),
            registration="manager" if helper == "" else "manager-helper",
            position=position,
        )
        record["accessExplicit"] = access_raw is not None
        return [record]

    def find_variable(self, expression):
        """(type, initializer) for a parameter object held in a variable, or None if ambiguous.

        The variable may be declared anywhere in the class body, including inside a method (statement
        splitting is depth-limited, so this scans the source directly). An object that is reassigned to
        different values cannot be attributed to one registration, so it is refused rather than guessed.
        """
        name = re.search(r"(" + IDENT + r")\s*$", expression.strip())
        if not name:
            return None
        variable = identifier(name.group(1))
        found = []
        for match in ASSIGNMENT_RE.finditer(self.body):
            if identifier(match.group("name")) != variable:
                continue
            parsed = parse_new_expression(match.group("rhs").strip(), match.group("type"))
            if parsed is not None:
                found.append(parsed)
        if not found:
            return None
        if any(item != found[0] for item in found[1:]):
            return None
        return found[0]

    def variable_optional(self, variable, position):
        """`v.Optional = true|false` set on a parameter object outside the registration call."""
        values = set()
        for match in re.finditer(r"\b" + re.escape(variable) + r"\s*\.\s*Optional\s*=\s*(true|false)\b", self.body):
            values.add(match.group(1) == "true")
        if len(values) > 1:
            self.reason("optionality of parameter variable '%s' is set to conflicting values (%s)" % (variable, position))
            return None
        return values.pop() if values else None

    # -- entry point ---------------------------------------------------------------------------
    def extract(self, class_body, reason_prefix=None):
        inputs_property = re.search(r"GH_SAMParam\s*\[\s*\]\s+Inputs\b", class_body)
        outputs_property = re.search(r"GH_SAMParam\s*\[\s*\]\s+Outputs\b", class_body)
        register_inputs = re.search(r"override\s+void\s+RegisterInputParams\s*\(", class_body)
        register_outputs = re.search(r"override\s+void\s+RegisterOutputParams\s*\(", class_body)

        if inputs_property and outputs_property:
            inputs_body = self.member_body(class_body, inputs_property.end(), accessor=True)
            outputs_body = self.member_body(class_body, outputs_property.end(), accessor=True)
            return self.declarative("input", inputs_body), self.declarative("output", outputs_body), "declarative"

        if register_inputs or register_outputs:
            if re.search(r"IGH_VariableParameterComponent", class_body):
                self.reason("variable-parameter component registered through the parameter manager: the insertable parameter set cannot be enumerated")
            inputs = self.manager("input", self.member_body(class_body, register_inputs.end(), accessor=False)) if register_inputs else []
            outputs = self.manager("output", self.member_body(class_body, register_outputs.end(), accessor=False)) if register_outputs else []
            if not register_inputs:
                self.reason("no RegisterInputParams override: the input set may be inherited")
            if not register_outputs:
                self.reason("no RegisterOutputParams override: the output set may be inherited")
            return inputs, outputs, "manager"

        if reason_prefix:
            self.reason(reason_prefix)
        if re.search(r"GH_SAMParam|AddParameter\s*\(|Add[A-Za-z_]+Parameter\s*\(|Params\s*\.\s*Register", class_body):
            self.reason("parameter declarations are present in the class but no declaration form was recognised: "
                        "the published parameter set cannot be established")
        return [], [], "inherited"

    def member_body(self, text, search_from, accessor):
        """The statement block of a member.

        A declared property is `{ get { <statements> } }`, so the outer brace has to be unwrapped
        before statements can be split - otherwise the whole body stays at depth 1 and no statement
        boundary is ever seen. A method body is already the statement block. An expression-bodied
        member (`=> [ new GH_SAMParam(...) ]`) has no brace at all and is taken up to its `;`.
        """
        rest = text[search_from:]
        arrow = re.match(r"\s*=>", rest)
        if accessor and arrow:
            body, _ = self.expression_body(rest, arrow.end())
            return body
        brace = text.find("{", search_from)
        if brace == -1:
            self.reason("a parameter declaration member has no body")
            return ""
        body, _ = take_balanced(text, brace)
        if not accessor:
            return body
        inner = re.match(r"\s*get\s*\{", body)
        if inner:
            inner_body, _ = take_balanced(body, body.index("{", inner.start()))
            return inner_body
        if re.match(r"\s*get\s*=>", body):
            arrow_rest = body[body.index("=>") + 2:]
            expression, _ = self.expression_body(arrow_rest, 0)
            return expression
        return body

    def expression_body(self, text, start):
        """The text of an expression-bodied member, up to the `;` that ends it (depth-aware)."""
        depth, i, instr, esc = 0, start, False, False
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
            elif ch in "([{":
                depth += 1
            elif ch in ")]}":
                depth -= 1
            elif ch == ";" and depth == 0:
                return text[start:i], i + 1
            i += 1
        return text[start:], len(text)


# --- component model ------------------------------------------------------------------------------------

def generic_substitutions(subclass, base_name, base_candidate):
    """{type parameter: closed type argument} for `Base<Arg>` in a subclass's base list.

    Returns None when the base is generic but the subclass does not supply a closed argument (for
    example the subclass is itself generic), so the inherited registration stays unresolved.
    """
    for part in split_top_level(subclass.get("base_text", "")):
        match = re.match(r"([\w.]+)\s*<\s*(.*)\s*>$", part.strip())
        if not match or simple_type(match.group(1)) != base_name:
            continue
        arguments = [simple_type(item.strip()) or item.strip() for item in split_top_level(match.group(2))]
        parameters = base_candidate.get("generics") or []
        if len(arguments) != len(parameters):
            return None
        own = set(subclass.get("generics") or [])
        if any(argument in own for argument in arguments):
            return None
        return dict(zip(parameters, arguments))
    return {}


def build_component(candidate, base_lookup, bases_by_class=None):
    """Extract one component, resolving inheritance from repository classes where it is unambiguous."""
    extractor = Extractor(candidate["class"], candidate["body"])
    inputs, outputs, registration = extractor.extract(candidate["body"])

    if registration == "inherited":
        for base in candidate["bases"]:
            if base in base_lookup:
                substitutions = generic_substitutions(candidate, base, base_lookup[base])
                if substitutions is None:
                    extractor.reason("base class %s is generic and is not closed with literal type arguments by %s"
                                     % (base, candidate["class"]))
                    break
                extractor = Extractor(candidate["class"], base_lookup[base]["body"], substitutions)
                inputs, outputs, registration = extractor.extract(base_lookup[base]["body"], reason_prefix=None)
                registration = "inherited:%s" % base
                break
        else:
            registration = "none"

    if bases_by_class is not None and registration != "declarative":
        if "IGH_VariableParameterComponent" in ancestry(candidate["class"], bases_by_class):
            extractor.reason("variable-parameter component whose parameter-manager registration does not enumerate the insertable parameter set")

    for side, params in (("input", inputs), ("output", outputs)):
        for param in params:
            if param.get("optional") is None:
                extractor.reason("optionality of %s '%s' is not determined" % (side, param.get("name")))
            if param.get("access") is None:
                extractor.reason("access of %s '%s' is not determined" % (side, param.get("name")))
            if param.get("name") in (None, ""):
                extractor.reason("a %s parameter has no literal name" % side)
            if param.get("type") is None:
                extractor.reason("runtime type of %s '%s' is not determined" % (side, param.get("name")))

    status = "verified"
    if not inputs and not outputs and registration in ("inherited", "manager"):
        status = "unresolved"
    if extractor.reasons:
        status = "unresolved" if status == "unresolved" else "partial"

    return {
        "class": candidate["class"],
        "guid": candidate["guid"],
        "source": candidate["source"],
        "registration": registration,
        "inputs": inputs,
        "outputs": outputs,
        "extraction": {"status": status, "reasons": sorted(extractor.reasons),
                       "externalTypes": sorted(extractor.external_types)},
    }


def component_metadata(candidate, component):
    """Name/nickname/category/subcategory/description/version read from the constructor."""
    constructor = re.search(r"\b" + candidate["class"] + r"\s*\(\s*\)\s*(?=:\s*base\()", candidate["body"])
    base = base_args(candidate["body"], constructor.end()) if constructor else None
    if base and base[-1].startswith("GH_ParamAccess"):
        base = base[:-1]
    version = VERSION_RE.search(candidate["body"])
    entry = dict(component)
    entry.update({
        "name": lit(base[0]) if base else None,
        "nickname": lit(base[1]) if base and len(base) > 1 else None,
        "category": lit(base[-2]) if base and len(base) >= 4 else None,
        "subcategory": lit(base[-1]) if base and len(base) >= 4 else None,
        "description": lit(base[2]) if base and len(base) >= 4 else None,
        "latestComponentVersion": version.group(1) if version else None,
    })
    return entry


def scan(repo):
    """The full model for a repository: components, exclusions, unclassified classes and projects."""
    candidates, gh_state = discover_projects(repo)

    scanned = []
    ancestry = []
    for path, info in sorted(candidates.items()):
        if info["error"] or info["is_test"]:
            continue
        ancestry.append((os.path.dirname(path), info["root"]))
        if path in gh_state:
            scanned.append((os.path.dirname(path), info["root"]))
    scanned.sort(key=lambda item: item[0])
    ancestry.sort(key=lambda item: item[0])

    components, parameter_classes, unclassified, all_classes = classify_classes(repo, scanned, ancestry)
    base_lookup = dict(all_classes)
    classification = {}
    for entry in all_classes.values():
        classification.setdefault(entry["class"], []).extend(entry["bases"])

    entries = []
    for candidate in components:
        try:
            entries.append(component_metadata(candidate, build_component(candidate, base_lookup, classification)))
        except Exception as error:  # a parser failure must be reported, never crash the run or pass silently
            entries.append({
                "class": candidate["class"],
                "guid": candidate["guid"],
                "source": candidate["source"],
                "registration": "unknown",
                "inputs": [],
                "outputs": [],
                "name": None,
                "nickname": None,
                "category": None,
                "subcategory": None,
                "description": None,
                "latestComponentVersion": None,
                "extraction": {"status": "unresolved",
                               "reasons": ["extraction failed: %s: %s" % (type(error).__name__, error)],
                               "externalTypes": []},
            })
    entries.sort(key=lambda item: item["class"])

    projects = []
    for path, info in sorted(candidates.items()):
        state = gh_state.get(path)
        evidence = list(info["direct"][:1]) or ([state] if state else []) or ([info["error"]] if info["error"] else [])
        if info["is_test"]:
            evidence = evidence + ["test project: not scanned"]
        projects.append({
            "name": info["name"],
            "source": os.path.relpath(path, repo).replace("\\", "/"),
            "grasshopper": "direct" if state == "direct" else "indirect" if state else "none",
            "scanned": bool(state) and not info["is_test"] and not info["error"],
            "evidence": evidence,
        })

    return {
        "repository": os.path.basename(repo),
        "components": entries,
        "externalParameterTypes": sorted({item for entry in entries
                                          for item in entry["extraction"].get("externalTypes", [])}),
        "parameterClasses": [{"class": item["class"], "source": item["source"],
                              "reason": item["kind_reason"]} for item in sorted(parameter_classes, key=lambda i: i["class"])],
        "unclassifiedClasses": [{"class": item["class"], "source": item["source"],
                                 "reason": item["kind_reason"]} for item in sorted(unclassified, key=lambda i: i["class"])],
        "projects": projects,
    }


# --- naming standard and its frozen legacy exceptions -------------------------------------------------

def base_name(name):
    return (name or "").strip("_")


def expected_name(name, side, optional, persistent_default):
    core = base_name(name)
    if side == "output":
        return core[:1].upper() + core[1:]
    if not optional:
        return "_" + core
    return ("_" + core + "_") if persistent_default is not None else (core + "_")


def deviations(components):
    """Identifiers whose declared name does not follow the SAM-BIM new-interface naming rules."""
    result = []
    for component in components:
        for side in ("inputs", "outputs"):
            for param in component[side]:
                if not param.get("name"):
                    continue
                expected = expected_name(param["name"], side[:-1], bool(param.get("optional")), param.get("persistentDefault"))
                if param["name"] != expected:
                    result.append("%s.%s.%s" % (component["class"], side[:-1], param["name"]))
    return sorted(result)


def description_notes(components):
    """Advisory description observations. These are not compatibility and are not enforced by default."""
    notes = []
    for component in components:
        for side in ("inputs", "outputs"):
            for param in component[side]:
                where = "%s.%s.%s" % (component["class"], side[:-1], param.get("name"))
                description = (param.get("description") or "").strip()
                if not description:
                    notes.append("%s: empty description" % where)
                elif not description[0].isupper():
                    notes.append("%s: description does not begin with a capital letter" % where)
                if "Humidty" in description:
                    notes.append("%s: known typo 'Humidty'" % where)
                if param.get("nickname") is not None and param.get("nickname") != param.get("name"):
                    notes.append("%s: NickName '%s' differs from Name (allowed; recorded, not enforced)"
                                 % (where, param.get("nickname")))
    return sorted(notes)


# --- comparison -----------------------------------------------------------------------------------------

FROZEN_FIELDS = ("name", "type", "nickname", "optional", "access", "visibility", "persistentDefault")
IDENTITY_FIELDS = ("guid", "name", "nickname", "category", "subcategory")


def compare(baseline, current):
    """Structured findings: compatibility (fatal), extraction (fatal), conventions (fatal), advisory.

    A baseline without `schemaVersion` is the v1 manifest: it is not rejected, but it cannot certify
    extraction, so an unresolved current component still fails.
    """
    findings = {"compatibility": [], "extraction": [], "conventions": [], "advisory": [], "manifest": []}
    legacy_baseline = baseline.get("schemaVersion") is None

    if not legacy_baseline and baseline.get("schemaVersion") != SCHEMA_VERSION:
        findings["manifest"].append("baseline schemaVersion %s is not supported (expected %s); regenerate it in a reviewed update"
                                    % (baseline.get("schemaVersion"), SCHEMA_VERSION))

    by_class = {component["class"]: component for component in baseline.get("components", [])}
    compared = 0

    for component in current["components"]:
        previous = by_class.pop(component["class"], None)
        if previous is None:
            findings["compatibility"].append("component added: %s (%s)" % (component["class"], component["guid"]))
            continue
        compared += 1

        for field in IDENTITY_FIELDS:
            if previous.get(field) != component.get(field):
                findings["compatibility"].append("%s.%s: %r -> %r" % (component["class"], field, previous.get(field), component.get(field)))

        if previous.get("latestComponentVersion") != component.get("latestComponentVersion"):
            findings["manifest"].append(
                "%s.latestComponentVersion: %r -> %r - regenerate the manifest in this reviewed change "
                "(an intended component version bump is not a compatibility violation)"
                % (component["class"], previous.get("latestComponentVersion"), component.get("latestComponentVersion")))

        for side in ("inputs", "outputs"):
            old = previous.get(side) or []
            new = component.get(side) or []
            old_names = [param.get("name") for param in old]
            new_names = [param.get("name") for param in new]
            if old_names != new_names:
                findings["compatibility"].append("%s.%s: names/order changed\n    baseline: %s\n    current:  %s"
                                                 % (component["class"], side, old_names, new_names))
                continue
            for before, after in zip(old, new):
                for field in FROZEN_FIELDS:
                    if before.get(field) != after.get(field):
                        findings["compatibility"].append("%s.%s.%s: %s %r -> %r"
                                                         % (component["class"], side, after.get("name"), field,
                                                            before.get(field), after.get(field)))

        if component["extraction"]["status"] != "verified":
            findings["extraction"].append("%s: extraction is %s (%s)"
                                          % (component["class"], component["extraction"]["status"],
                                             "; ".join(component["extraction"]["reasons"]) or "no reason recorded"))

        baseline_extraction = previous.get("extraction")
        if baseline_extraction is not None and baseline_extraction.get("status") != "verified":
            findings["extraction"].append("%s: the approved baseline itself records an incomplete extraction (%s); "
                                          "regenerate it in a reviewed change"
                                          % (component["class"], baseline_extraction.get("status")))

    for leftover in sorted(by_class):
        findings["compatibility"].append("component removed: %s" % leftover)

    expected = sorted(baseline.get("namingExceptions", []))
    actual = deviations(current["components"])
    if expected != actual:
        added = [item for item in actual if item not in expected]
        stale = [item for item in expected if item not in actual]
        if added:
            findings["conventions"].append("new naming-standard deviation(s) not in the approved exception list: %s" % added)
        if stale:
            findings["conventions"].append("approved naming exception(s) that are no longer deviations: %s" % stale)

    findings["advisory"].extend(description_notes(current["components"]))
    findings["_compared"] = compared
    return findings


def summarise(model):
    counts = {"verified": 0, "partial": 0, "unresolved": 0}
    for component in model["components"]:
        counts[component["extraction"]["status"]] += 1
    projects = model.get("projects", [])
    return {
        "components": len(model["components"]),
        "verified": counts["verified"],
        "partial": counts["partial"],
        "unresolved": counts["unresolved"],
        "parameterClassesExcluded": len(model.get("parameterClasses", [])),
        "unclassifiedClasses": len(model.get("unclassifiedClasses", [])),
        "projectsScanned": sum(1 for project in projects if project.get("scanned")),
        "projectsDirect": sum(1 for project in projects if project["grasshopper"] == "direct" and project.get("scanned")),
        "projectsIndirect": sum(1 for project in projects if project["grasshopper"] == "indirect" and project.get("scanned")),
        "projectsTotal": len(projects),
    }


# --- entry point ----------------------------------------------------------------------------------------

def manifest_path(repo):
    return os.path.join(repo, "design", "grasshopper-icons", "param_contract.json")


def main(argv):
    here = os.path.dirname(os.path.abspath(__file__))
    default_repo = os.path.dirname(os.path.dirname(os.path.dirname(here)))

    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--repo", default=default_repo, help="repository to scan (default: the SAM repository this file lives in)")
    parser.add_argument("--output", default=None, help="manifest to write (default: <repo>/design/grasshopper-icons/param_contract.json)")
    parser.add_argument("--check", action="store_true", help="compatibility gate: compare the source against the committed manifest")
    parser.add_argument("--strict-descriptions", action="store_true", help="treat description-quality observations as failures")
    parser.add_argument("--show-conventions", action="store_true", help="list every naming-rule deviation found")
    parser.add_argument("--json", action="store_true", help="print a machine-readable summary")
    args = parser.parse_args(argv[1:])

    repo = os.path.abspath(args.repo)
    if not os.path.isdir(repo):
        print("FAIL no such repository: %s" % repo)
        return 2

    model = scan(repo)
    model["schemaVersion"] = SCHEMA_VERSION
    model["namingExceptions"] = deviations(model["components"])
    summary = summarise(model)

    if args.json:
        print(json.dumps({"repository": model["repository"], "summary": summary,
                          "components": [{"class": c["class"], "status": c["extraction"]["status"],
                                          "reasons": c["extraction"]["reasons"], "inputs": len(c["inputs"]),
                                          "outputs": len(c["outputs"]), "source": c["source"]}
                                         for c in model["components"]]}, indent=1))

    if not args.check:
        output = os.path.abspath(args.output) if args.output else manifest_path(repo)
        directory = os.path.dirname(output)
        if directory and not os.path.isdir(directory):
            os.makedirs(directory)
        with open(output, "w", encoding="utf-8", newline="\n") as stream:
            json.dump(model, stream, indent=1, ensure_ascii=False)
            stream.write("\n")
        print("inventory %s -> %s" % (model["repository"], output))
        print("  components %(components)d (verified %(verified)d, partial %(partial)d, unresolved %(unresolved)d)" % summary)
        print("  parameter classes excluded %(parameterClassesExcluded)d, unclassified classes %(unclassifiedClasses)d" % summary)
        print("  projects %(projectsTotal)d (direct %(projectsDirect)d, indirect %(projectsIndirect)d)" % summary)
        print("  naming deviations %d, description notes %d" % (len(model["namingExceptions"]), len(description_notes(model["components"]))))
        print("  NOT compatibility validation: no approved baseline was read.")
        if args.show_conventions:
            for item in model["namingExceptions"]:
                print("  naming deviation %s" % item)
        if summary["unresolved"] or summary["partial"]:
            print("  unresolved/partial components are listed in the manifest under extraction.reasons")
        return 0

    path = manifest_path(repo)
    if not os.path.isfile(path):
        print("FAIL no approved baseline at %s" % path)
        print("      Run an inventory to review a proposed baseline, then commit it in a reviewed change:")
        print("      python %s --repo %s" % (os.path.relpath(__file__), repo))
        return 2

    with open(path, encoding="utf-8") as stream:
        baseline = json.load(stream)

    findings = compare(baseline, model)
    fatal = []
    for category in ("compatibility", "extraction", "conventions", "manifest"):
        for message in findings[category]:
            fatal.append((category, message))
    for message in findings["advisory"]:
        print("NOTE advisory: %s" % message)
    if args.strict_descriptions and findings["advisory"]:
        fatal.extend(("advisory", message) for message in findings["advisory"])

    for category, message in fatal:
        print("FAIL %s: %s" % (category, message))

    print("%s: %d components discovered, %d fully verified, %d partial, %d unresolved; %d compared against the approved baseline; %d parameter classes excluded"
          % (model["repository"], summary["components"], summary["verified"], summary["partial"],
             summary["unresolved"], findings["_compared"], summary["parameterClassesExcluded"]))
    if not fatal:
        print("OK compatibility (baseline %s)" % os.path.relpath(path, repo).replace("\\", "/"))
        return 0
    print("%d PROBLEM(S)" % len(fatal))
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
