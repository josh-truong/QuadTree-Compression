using System.Runtime.Versioning;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace QuadTreeCompression.Controllers
{
    /// <summary>
    /// Struct <c>RGB</c> color model for a pixel.
    /// Size 3 bytes.
    /// </summary>
    readonly public struct RGB
    {
        /// <summary>Instance variable <c>r</c> represents the color Red.</summary>
        public byte R { get; }
        /// <summary>Instance variable <c>g</c> represents the color Green.</summary>
        public byte G { get; }
        /// <summary>Instance variable <c>b</c> represents the color Blue.</summary>
        public byte B { get; }

        /// <summary>
        /// Struct <c>RGB</c> color model for a pixel. Size 3 bytes.
        /// <param name="r">Red.</param>
        /// <param name="g">Green.</param>
        /// <param name="b">Blue.</param>
        /// </summary>
        public RGB(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    /// <summary>
    /// Struct <c>Bbox</c> models the bounding box of the image.
    /// Size 16 bytes.
    /// </summary>
    readonly public struct BBox
    {
        /// <summary>Instance variable <c>x0</c> represents the x-point for (x0,y0).</summary>
        public int X { get; }
        /// <summary>Instance variable <c>y0</c> represents the y-point for (x0,y0).</summary>
        public int Y { get; }
        /// <summary>Instance variable <c>width</c> represents the width calculated.</summary>
        public int Width { get; }
        /// <summary>Instance variable <c>height</c> represents the height calculated.</summary>
        public int Height { get; }

        /// <summary>
        /// <c>Bbox</c> Constructor.
        /// <param name="X">X.</param>
        /// <param name="Y">Y.</param>
        /// <param name="Width">Width.</param>
        /// <param name="Height">Height.</param>
        /// </summary>
        public BBox(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        /// <summary>
        /// Checks if the bounding box is greater than a pixel.
        /// </summary>
        /// <returns>Bool</returns>
        public bool IsValid()
        {
            return (Width * Height) > 1;
        }
    }

    [SupportedOSPlatform("windows")]
    public class Histogram
    {
        public readonly int[] R = new int[256];
        public readonly int[] G = new int[256];
        public readonly int[] B = new int[256];

        public Histogram(ref Bitmap bmp)
        {
            // Time complexity: O(n)
            int width = bmp.Width;
            int num_pixels = width * bmp.Height;

            for (int i = 0; i < num_pixels; i++)
            {
                int x = i % width;
                int y = i / width;

                Color c = bmp.GetPixel(x, y);
                R[c.R] += 1;
                G[c.G] += 1;
                B[c.B] += 1;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public class ImageDetail
    {
        public readonly RGB color;
        public readonly float error;

        public ImageDetail(ref Bitmap bmp)
        {
            Histogram histo = new(ref bmp);
            (byte r, float re) = WeightedAverage(histo.R);
            (byte g, float ge) = WeightedAverage(histo.G);
            (byte b, float be) = WeightedAverage(histo.B);
            error = re * 0.2989f + ge * 0.5870f + be * 0.1140f;
            color = new RGB(r, g, b);
        }

        public Tuple<byte, float> WeightedAverage(int[] hist)
        {
            int total = hist.Sum();
            int value = 0;
            float error = 0.0f;

            if (total > 0)
            {
                for (int i = 0; i < hist.Length; i++)
                    value += i * hist[i];
                value /= total;

                for (int i = 0; i < hist.Length; i++)
                    error += hist[i] * ((value - i) * (value - i));
                error /= total;
                error = MathF.Sqrt(error);
            }
            return Tuple.Create<byte, float>((byte)value, error);
        }
    }



    [SupportedOSPlatform("windows")]
    public class Node
    {
        private readonly int Depth;
        private bool Leaf;

        public readonly BBox Bbox;
        public readonly RGB color;
        public readonly float error;

        public Node? Node_0 { get; set; }
        public Node? Node_1 { get; set; }
        public Node? Node_2 { get; set; }
        public Node? Node_3 { get; set; }

        public Node(ref Bitmap im, BBox bbox, int depth)
        {
            Bbox = bbox;
            Depth = depth;

            // Create drawing target
            Bitmap bmp = new(bbox.Width, bbox.Height, im.PixelFormat);
            Graphics graphics = Graphics.FromImage(bmp);

            // Create rectangle for source image
            Rectangle srcRect = new(bbox.X, bbox.Y, bbox.Width, bbox.Height);
            graphics.DrawImage(im, 0, 0, srcRect, GraphicsUnit.Pixel);

            ImageDetail get_detail = new(ref bmp);
            color = get_detail.color;
            error = get_detail.error;
        }

        public int GetDepth() { return Depth; }

        public bool GetLeaf() { return Leaf; }
        public void SetLeaf() { Leaf = true; }
    }

    [SupportedOSPlatform("windows")]
    internal class QuadTree
    {
        private readonly Node root;

        public readonly int width;
        public readonly int height;
        public readonly int MaxDepth;
        public readonly int Threshold;
        public readonly PixelFormat format;

        public int TreeHeight { get; private set; }

        public QuadTree(ref Bitmap im, int max_depth = 10, int threshold = 13)
        {
            BBox bbox = new(0, 0, im.Width, im.Height);
            root = new Node(ref im, bbox, 0);
            width = im.Width;
            height = im.Height;
            MaxDepth = max_depth;
            TreeHeight = 0;
            Threshold = threshold;
            format = im.PixelFormat;

            Build(root, ref im);
        }

        private void Build(Node node, ref Bitmap bmp)
        {
            if (node.GetDepth() >= this.MaxDepth || node.error <= Threshold)
            {
                if (node.GetDepth() > this.TreeHeight)
                    this.TreeHeight = node.GetDepth();
                node.SetLeaf();
                return;
            }
            // Subdivide node
            Subdivide(ref node, ref bmp);

            // Recurse node
            if (node.Node_0 != null)
                Build(node.Node_0, ref bmp);
            if (node.Node_1 != null)
                Build(node.Node_1, ref bmp);
            if (node.Node_2 != null)
                Build(node.Node_2, ref bmp);
            if (node.Node_3 != null)
                Build(node.Node_3, ref bmp);
        }

        private static void Subdivide(ref Node node, ref Bitmap bmp)
        {
            int X, Y, width, height, Mid_X, Mid_Y, Mid_W, Mid_H;
            BBox bbox, bbox_0, bbox_1, bbox_2, bbox_3;

            bbox = node.Bbox;

            X = bbox.X;
            Y = bbox.Y;
            width = bbox.Width;
            height = bbox.Height;

            Mid_W = (int)Math.Ceiling(width / 2.0f);
            Mid_H = (int)Math.Ceiling(height / 2.0f);
            Mid_X = X + Mid_W;
            Mid_Y = Y + Mid_H;

            bbox_0 = new(X, Y, Mid_W, Mid_H);
            bbox_1 = new(Mid_X, Y, Mid_W, Mid_H);
            bbox_2 = new(X, Mid_Y, Mid_W, Mid_H);
            bbox_3 = new(Mid_X, Mid_Y, Mid_W, Mid_H);

            if (bbox_0.IsValid())
                node.Node_0 = new(ref bmp, bbox_0, node.GetDepth() + 1); // 2nd quadrant
            if (bbox_1.IsValid())
                node.Node_1 = new(ref bmp, bbox_1, node.GetDepth() + 1); // 1st quadrant
            if (bbox_2.IsValid())
                node.Node_2 = new(ref bmp, bbox_2, node.GetDepth() + 1); // 3rd quadrant
            if (bbox_3.IsValid())
                node.Node_3 = new(ref bmp, bbox_3, node.GetDepth() + 1); // 4th quadrant
        }

        private List<Node> GetLeafNodes(int depth)
        {
            static void GetLeafNodesHelper(Node node, int depth, Action<Node> add)
            {
                if (node.GetLeaf() || node.GetDepth() == depth)
                    add(node);
                else
                {
                    if (node.Node_0 != null)
                        GetLeafNodesHelper(node.Node_0, depth, add);
                    if (node.Node_1 != null)
                        GetLeafNodesHelper(node.Node_1, depth, add);
                    if (node.Node_2 != null)
                        GetLeafNodesHelper(node.Node_2, depth, add);
                    if (node.Node_3 != null)
                        GetLeafNodesHelper(node.Node_3, depth, add);
                }
            }
            List<Node> leaf_nodes = new();
            GetLeafNodesHelper(root, depth, leaf_nodes.Add);
            return leaf_nodes;
        }

        public Bitmap CreateImage(int depth, bool show_lines = false, bool show_edge = false)
        {
            BBox bbox = root.Bbox;
            // Create drawing target
            Bitmap bmp = new(bbox.Width, bbox.Height, format);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 0)), new(bbox.X, bbox.Y, bbox.Width, bbox.Height));

            List<Node> leaf_nodes = GetLeafNodes(depth);
            foreach (Node node in leaf_nodes)
            {
                Color color;
                if (show_edge)
                    color = Color.FromArgb(255, 255, 255);
                else
                    color = Color.FromArgb(node.color.R, node.color.G, node.color.B);

                Rectangle rect = new(node.Bbox.X, node.Bbox.Y, node.Bbox.Width, node.Bbox.Height);
                SolidBrush brush = new(color);

                Pen pen = new(Color.Black, 1)
                {
                    Alignment = PenAlignment.Inset
                };

                if (node.GetLeaf() && show_edge)
                    graphics.FillRectangle(brush, rect);
                else if (!show_edge)
                    graphics.FillRectangle(brush, rect);

                if (show_lines)
                    graphics.DrawRectangle(pen, rect);
            }
            return bmp;
        }

        public void GetMeta(int depth)
        {
            List<Node> leaf_nodes = GetLeafNodes(depth);
            int drawn_nodes = leaf_nodes.Count;
            double error = 0;
            foreach (Node node in leaf_nodes)
                error += node.error;

            Debug.WriteLine("------------------------------------------------");
            Debug.WriteLine("MaxDepth: {0}. TreeHeight {1}", MaxDepth, TreeHeight);
            Debug.WriteLine("{0} nodes drawn.", drawn_nodes);
            Debug.WriteLine("Error Threshold: {0}", Threshold);
            Debug.WriteLine("Distortion Error: {0}. Average Distortion error: {1}", error, error / drawn_nodes);
        }

        public int[] ObjectToArray(int depth)
        {
            // X, Y, Width, Height, R, G, B
            int offset = 7;
            List<Node> leaf_nodes = GetLeafNodes(depth);
            int num_nodes = leaf_nodes.Count;
            int num_elements = num_nodes * offset;
            int [] array = new int[num_elements];
            for (int i = 0; i < num_elements; i+=offset)
            {
                Node node = leaf_nodes[i/offset];
                BBox bbox = node.Bbox;
                RGB rgb = node.color;
                int x = bbox.X;
                int y = bbox.Y;
                int width = bbox.Width;
                int height = bbox.Height;
                int r = (int)rgb.R;
                int g = (int)rgb.G;
                int b = (int)rgb.B;

                array[i    ] = x;
                array[i + 1] = y;
                array[i + 2] = width;
                array[i + 3] = height;
                array[i + 4] = r;
                array[i + 5] = g;
                array[i + 6] = b;
            }
            return array;
        }
    }
}