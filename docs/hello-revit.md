# 👋 Hello Revit: Your First RScript

This tutorial walks you through writing and running your first **RScript** — a lightweight C# script executed live inside Revit using the RScripting framework.

You’ll learn how to:

- Use `Print(...)` to send messages to VS Code’s output tab  
- Access global Revit objects (`Doc`, `UIDoc`, `UIApp`)  
- Safely create geometry using `Transact(...)`

---

## 🧭 Prerequisites

Make sure you’ve completed the [Getting Started](getting-started.md) guide and have:

- Revit 2025 open with a project  
- The **RScript Server** toggle ON in the Add-Ins tab  
- A scripting workspace initialized in VS Code

---

## 📝 Step 1: Edit `Main.cs`

Open:

```
Scripts/Main.cs
```

Replace the content with:

```csharp
using Autodesk.Revit.DB;

// 🌍 Define wall size in meters
double wallLengthMeters = 6.0;
double wallHeightMeters = 3.0;

// 🧮 Convert to Revit internal units (feet)
double lengthFt = UnitUtils.ConvertToInternalUnits(wallLengthMeters, UnitTypeId.Meters);
double heightFt = UnitUtils.ConvertToInternalUnits(wallHeightMeters, UnitTypeId.Meters);

// 📍 Define wall line endpoints (centered at origin)
XYZ pt1 = new XYZ(-lengthFt / 2, 0, 0);
XYZ pt2 = new XYZ(lengthFt / 2, 0, 0);
Line wallLine = Line.CreateBound(pt1, pt2);

// 🧭 Find base level
Level? level = new FilteredElementCollector(Doc)
    .OfClass(typeof(Level))
    .Cast<Level>()
    .FirstOrDefault(l => l.Name == "Level 1");

if (level == null)
{
    Print("❌ Level 'Level 1' not found.");
    return;
}

Print("📌 Creating wall centered at origin...");

Transact("Create wall", doc =>
{
    Wall wall = Wall.Create(doc, wallLine, level.Id, false);
    wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.Set(heightFt);

    Print("✅ Wall created.");
});
```

---

## ▶️ Step 2: Send Script to Revit

Make sure Revit’s **RScript Server** is ON.

In VS Code:

```
Ctrl + Shift + P → RScript: Send To Revit
```

Or press:

```
Ctrl + Alt + R
```

---

## ✅ View the Result

In VS Code's Output tab (channel: `RScript`), you’ll see:

```
[PRINT hh:mm:ss] 📌 Creating wall centered at origin...
[PRINT hh:mm:ss] ✅ Wall created.
```

You’ll also see the wall appear in Revit, placed along the X-axis and centered at the origin.

---

## 🧠 How It Works

- `Print(...)` logs timestamped messages to the output tab  
- Geometry calculations happen before `Transact(...)`  
- `Transact(...)` wraps only operations that modify the document  
- All units are defined in meters and safely converted

---

## 🧪 Experiment

Try:

- Rotating the line by modifying `pt1` and `pt2` Y values  
- Switching to `"Level 2"` if available  
- Setting the height to 5 or 10 meters

---

Built with ❤️ by [Seyoum Hagos](https://github.com/Sey56)
