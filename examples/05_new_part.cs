// Replace the path with a new file path on your own machine.
// This creates a part in the connected NX window, but does NOT save it.
string path = @"C:\NX-work\example.prt";
session.Parts.NewDisplay(path, NXOpen.Part.Units.Millimeters);
result = session.Parts.Work.Name;
