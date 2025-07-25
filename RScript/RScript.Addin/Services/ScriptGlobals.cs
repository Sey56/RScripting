﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

namespace RScript.Addin.Services
{
    public static class ScriptGlobals
    {
        public static UIApplication? UIApp { get; set; }
        public static UIDocument? UIDoc { get; set; }
        public static Document? Doc { get; set; }

        public static UIApplication? uiapp => UIApp;
        public static UIDocument? uidoc => UIDoc;
        public static Document? doc => Doc;

        public static List<string> PrintLogs { get; } = new();

        public static void Print(string message)
        {
            var entry = $"[PRINT {DateTime.Now:HH:mm:ss}] {message}";
            PrintLogs.Add(entry);
            // Console.WriteLine(entry); // suppressed for Revit runtime
        }
    }
}