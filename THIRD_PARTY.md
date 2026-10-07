# Third-party components

## PDFsharp and MigraDoc
- Package: `PDFsharp-MigraDoc` 6.2.0 from NuGet, with its dependency `PDFsharp` 6.2.0.
- License: MIT. Copyright (c) 2005-2024 empira Software GmbH, Troisdorf (Cologne Area), Germany.
- Used by: `SAM/SAM.Core.Reporting.Pdf`, the PDF renderer of the reporting framework.
- Distribution: a package reference only. No PDFsharp or MigraDoc source is included in this repository.

## Noto Sans
- Files: `SAM/SAM.Core.Reporting.Pdf/Fonts/NotoSans-Regular.ttf` and `NotoSans-Bold.ttf`, the hinted static TTFs
  from https://github.com/notofonts/notofonts.github.io (`fonts/NotoSans/hinted/ttf`).
  - SHA-256 (Regular): `478c558ea716033cd60c03438f628dfa75694dcf6b5f6d505a2f05fd2b4f3823`.
  - SHA-256 (Bold): `1df075a380fc7cb898acf64c1f7b3b4dd780de3caa860178bf929de35817a913`.
- License: SIL Open Font License 1.1. Copyright 2022 The Noto Project Authors. The license text is in
  `SAM/SAM.Core.Reporting.Pdf/Fonts/OFL.txt`.
- Use: the fonts are embedded unmodified as resources in `SAM.Core.Reporting.Pdf.dll`. A subset is embedded in each PDF
  the renderer writes; the OFL permits this.
- Distribution: the OFL requires the license to accompany the font software. An installer that ships
  `SAM.Core.Reporting.Pdf.dll` should also ship `OFL.txt`.

## GenOpt 3.1.1 (behavioural reference; no code included)
- What: GenOpt, the Generic Optimization Program by Lawrence Berkeley National Laboratory. Version 3.1.1
  (`genopt.jar` built 2016-03-30), as shipped with Tas Generic Optimisation.
- Relationship: the optimisation kernel in `SAM/SAM.Math/Classes/Optimisation` (`HookeJeeves`, `CoordinateSearch`,
  `GoldenSection`) is behaviour-compatible with GenOpt 3.1.1. The implementation was written independently from the
  behavioural specification `documentation/GenOpt-3.1.1-Behaviour.md`, and no GenOpt source code was copied. The
  specification was derived from reading the GenOpt 3.1.1 source and from black-box traces of the Java `genopt.jar`.
- License (GenOpt's): a 3-clause BSD-style license with an additional "Enhancements" paragraph and a U.S. Department of
  Energy notice; it appears to match SPDX `BSD-3-Clause-LBNL`. The text is in `legal.html` inside `genopt.jar`.
  Copyright line: "GenOpt Copyright (c) 1998-2016, The Regents of the University of California, through Lawrence
  Berkeley National Laboratory (subject to receipt of any required approvals from the U.S. Dept. of Energy). All
  rights reserved."
- Test fixtures: `SAM/SAM.Tests/Golden/GenOpt/` holds sanitised output data rows from synthetic GenOpt runs: numbers,
  listing comments and termination text. It has no GenOpt banner, copyright header, console transcript or code.
- Distribution: no GenOpt code, `genopt.jar` or Java runtime is included in this repository or its builds. The oracle
  tool `SAM/SAM.Tests.GenOptOracle` runs a locally supplied `genopt.jar`, which is never committed.
