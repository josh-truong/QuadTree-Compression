### How to use QuadTree.cs

```cs
string filename_i = "cosmic_cliffs.jpg";
string filename_o = "out.png";
string filepath = "";
string filepath_i = String.Format("{0}/{1}", filepath, filename_i);
string filepath_o = String.Format("{0}/{1}", filepath, filename_o);

try
{
    Image im = (Bitmap)Image.FromFile(filepath_i);
    Bitmap bmp = new(im);

    Debug.WriteLine("Building ...");
    QuadTree qtree = new(ref bmp, max_depth: 10, threshold: 15);
    int depth = qtree.TreeHeight;

    Bitmap bmp_o = qtree.CreateImage(depth, show_lines: false, show_edge: false);
    bmp_o.Save(filepath_o, ImageFormat.Png);

    // Get information about quadtree and desired depth
    qtree.GetMeta(depth);

    // Convert quadtree data into an int array
    // Position of elements within array, every 28 bytes represents a node: the bounding box (x,y,width, height), and color (rgb)
    // 28 bytes (7 elements * type size (Int = 4 bytes))
    // x ((i*7)+0), y ((i*7)+1), width ((i*7)+2), height ((i*7)+3), r ((i*7)+4), g ((i*7)+5), b ((i*7)+6)
    int[] array = qtree.ObjectToArray(depth);
}
catch (Exception e) { Debug.WriteLine(e); }
```