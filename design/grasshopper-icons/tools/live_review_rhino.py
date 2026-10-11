# -*- coding: utf-8 -*-
# Live visual review harness for SAM #166 (runs inside Rhino 8 via _-RunPythonScript, IronPython).
# Places representative SAM components on a GH canvas at 1:1 and captures the real rendering in the
# default (light) skin and a dark skin, plus the SAM ribbon. Restores all GH settings afterwards.
import os, json, System, traceback
import Rhino

# Usage: put items.json (list of {label, items:[{guid,name,icon}]}) in a folder without spaces, set
#   SAM_GH_LIVE_DIR to it, then: Rhino.exe /nosplash /runscript="_-RunPythonScript <that folder>\live_review_rhino.py"
# Close Rhino afterwards. GH skin / icon-display settings are restored before the script ends.
OUT = os.environ.get("SAM_GH_LIVE_DIR") or os.path.dirname(os.path.abspath(__file__))
log = open(os.path.join(OUT, "log.txt"), "w")


def L(*a):
    log.write(" ".join(str(x) for x in a) + "\n")
    log.flush()


def pump(n=5, wait_ms=0):
    for _ in range(n):
        System.Windows.Forms.Application.DoEvents()
    if wait_ms:
        System.Threading.Thread.Sleep(wait_ms)
        for _ in range(n):
            System.Windows.Forms.Application.DoEvents()


def capture(ctrl, path):
    """Screen-grab a visible WinForms control (what the user actually sees)."""
    from System.Drawing import Bitmap, Graphics, Point, Size
    ctrl.Refresh()
    pump()
    pt = ctrl.PointToScreen(Point(0, 0))
    bmp = Bitmap(ctrl.Width, ctrl.Height)
    g = Graphics.FromImage(bmp)
    g.CopyFromScreen(pt, Point(0, 0), Size(ctrl.Width, ctrl.Height))
    g.Dispose()
    bmp.Save(path)
    L("captured", path, ctrl.Width, ctrl.Height)


def find_ribbon(ctrl):
    if ctrl.GetType().Name == "GH_Ribbon":
        return ctrl
    for c in ctrl.Controls:
        r = find_ribbon(c)
        if r is not None:
            return r
    return None


state = {}
try:
    gh = Rhino.RhinoApp.GetPlugInObject("Grasshopper")
    gh.LoadEditor()
    gh.ShowEditor()
    pump(20)
    import clr
    clr.AddReference("Grasshopper")
    from Grasshopper import Instances, CentralSettings
    from Grasshopper.Kernel import GH_Document
    from Grasshopper.Kernel.Special import GH_Scribble
    from Grasshopper.GUI.Canvas import GH_Skin, GH_PaletteStyle
    from System.Drawing import PointF, Color

    items = json.load(open(os.path.join(OUT, "items.json")))

    # 1. the icons exactly as Grasshopper loaded them from the installed .gha files
    for row in items:
        for it in row["items"]:
            proxy = Instances.ComponentServer.EmitObjectProxy(System.Guid(it["guid"]))
            if proxy is None:
                L("NO PROXY", it["name"])
                continue
            ic = proxy.Icon
            ic.Save(os.path.join(OUT, "loaded_" + it["icon"] + ".png"))
            L("proxy", it["name"], "|", proxy.Desc.Category, "/", proxy.Desc.SubCategory, "|", ic.Width, "x", ic.Height,
              "|", proxy.Location)

    # 2. canvas with components, icon display on
    state["icons"] = CentralSettings.CanvasObjectIcons
    CentralSettings.CanvasObjectIcons = True
    doc = GH_Document()
    X0, DX, ROW = 40, 300, 360
    for r, row in enumerate(items):
        s = GH_Scribble()
        s.Text = row["label"]
        s.CreateAttributes()
        s.Attributes.Pivot = PointF(X0, r * ROW + 10)
        doc.AddObject(s, False)
        for c, it in enumerate(row["items"]):
            obj = Instances.ComponentServer.EmitObject(System.Guid(it["guid"]))
            if obj is None:
                L("EMIT FAILED", it["name"])
                continue
            obj.CreateAttributes()
            obj.Attributes.Pivot = PointF(X0 + 40 + c * DX, r * ROW + 60)
            obj.Attributes.ExpireLayout()
            doc.AddObject(obj, False)
            L("placed", it["name"], obj.GetType().FullName)
    Instances.DocumentServer.AddDocument(doc)
    canvas = Instances.ActiveCanvas
    canvas.Document = doc
    ed = Instances.DocumentEditor
    ed.WindowState = System.Windows.Forms.FormWindowState.Maximized
    ed.BringToFront()
    ed.Activate()
    pump(20)

    ribbon = find_ribbon(ed)
    if ribbon is not None:
        L("ribbon members:", [m.Name for m in ribbon.GetType().GetMembers() if "Tab" in m.Name])
        try:
            ribbon.ActiveTabName = "SAM"
            L("ribbon tab set via ActiveTabName")
        except Exception as ex:
            L("ActiveTabName failed:", ex)
            try:
                for t in ribbon.Tabs:
                    if t.NameFull == "SAM":
                        ribbon.ActiveTab = t
                        L("ribbon tab set via ActiveTab")
            except Exception as ex2:
                L("ActiveTab failed:", ex2)
        pump(10)

    placed = [o for o in doc.Objects if o.GetType().Name != "GH_Scribble"]

    def shoot(tag):
        # park the mouse over an empty canvas corner so no ribbon dropdown / tooltip is open
        from System.Drawing import Point as SPoint
        System.Windows.Forms.Cursor.Position = canvas.PointToScreen(SPoint(canvas.Width - 5, canvas.Height - 5))
        pump(5, 800)
        for r in range(len(items)):
            for half in (0, 1):
                canvas.Viewport.Zoom = 1.0
                left = X0 + 40 + half * 4 * DX - 30
                canvas.Viewport.MidPoint = PointF(left + canvas.Width / 2.0, r * ROW + 20 + canvas.Height / 2.0)
                canvas.Refresh()
                pump(5, 400)
                name = "canvas_%s_row%d_%d.png" % (tag, r, half)
                capture(canvas, os.path.join(OUT, name))
                if tag == "light":
                    for o in placed:
                        b = o.Attributes.Bounds
                        p = canvas.Viewport.ProjectPoint(PointF(b.X, b.Y))
                        L("bounds", name, o.Name, round(p.X), round(p.Y), round(b.Width), round(b.Height), o.RuntimeMessageLevel)
        if ribbon is not None:
            pump(5, 1500)
            capture(ribbon, os.path.join(OUT, "ribbon_%s.png" % tag))

    shoot("light")

    # 3. dark skin (restored afterwards)
    names = ["canvas_back", "canvas_grid", "canvas_edge", "canvas_shade",
             "palette_normal_standard", "palette_normal_selected", "palette_hidden_standard", "palette_hidden_selected"]
    for n in names:
        state[n] = getattr(GH_Skin, n)
    GH_Skin.canvas_back = Color.FromArgb(40, 40, 40)
    GH_Skin.canvas_grid = Color.FromArgb(60, 255, 255, 255)
    GH_Skin.canvas_edge = Color.FromArgb(20, 20, 20)
    GH_Skin.canvas_shade = Color.FromArgb(80, 0, 0, 0)
    GH_Skin.palette_normal_standard = GH_PaletteStyle(Color.FromArgb(70, 70, 70), Color.FromArgb(15, 15, 15), Color.FromArgb(235, 235, 235))
    GH_Skin.palette_normal_selected = GH_PaletteStyle(Color.FromArgb(60, 110, 60), Color.FromArgb(15, 40, 15), Color.FromArgb(235, 235, 235))
    GH_Skin.palette_hidden_standard = GH_PaletteStyle(Color.FromArgb(55, 55, 55), Color.FromArgb(15, 15, 15), Color.FromArgb(235, 235, 235))
    GH_Skin.palette_hidden_selected = GH_PaletteStyle(Color.FromArgb(60, 110, 60), Color.FromArgb(15, 40, 15), Color.FromArgb(235, 235, 235))
    pump(5)
    shoot("dark")
    L("DONE")
except Exception:
    L("ERROR", traceback.format_exc())
finally:
    try:
        from Grasshopper import Instances, CentralSettings
        from Grasshopper.GUI.Canvas import GH_Skin
        for k, v in state.items():
            if k == "icons":
                CentralSettings.CanvasObjectIcons = v
            else:
                setattr(GH_Skin, k, v)
        L("restored", sorted(state.keys()))
        try:
            d = Instances.ActiveCanvas.Document
            if d is not None:
                d.Enabled = False
                Instances.DocumentServer.RemoveDocument(d)
        except Exception as ex:
            L("doc cleanup:", ex)
    except Exception:
        L("restore ERROR", traceback.format_exc())
    log.close()
    open(os.path.join(OUT, "finished.txt"), "w").write("ok")
