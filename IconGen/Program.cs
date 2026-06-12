using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

const string outputPath = @"C:\Users\bilgiislem.mdr\LiveSession\LiveSession.UI\Assets\livesession.ico";

int[] iconSizes = { 256, 128, 64, 48, 32, 16 };
var bitmaps = new List<Bitmap>();

foreach (var size in iconSizes)
    bitmaps.Add(DrawIcon(size));

SaveIco(bitmaps, outputPath);
foreach (var b in bitmaps) b.Dispose();
Console.WriteLine($"Icon saved: {outputPath}");

// ─── Draw ────────────────────────────────────────────────────────────────────

Bitmap DrawIcon(int size)
{
    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode      = SmoothingMode.AntiAlias;
    g.CompositingQuality = CompositingQuality.HighQuality;
    g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode    = PixelOffsetMode.HighQuality;
    g.Clear(Color.Transparent);

    float s  = size;
    float cx = s / 2f;
    float cy = s / 2f;

    // ── Background rounded square ─────────────────────────────────────────
    float bgM = s * 0.04f;
    float bgR = s * 0.20f;
    var bgRect = new RectangleF(bgM, bgM, s - 2 * bgM, s - 2 * bgM);

    using var bgPath = RoundedRect(bgRect, bgR);
    using (var bgGrad = new LinearGradientBrush(
        new PointF(0, 0), new PointF(s, s),
        Color.FromArgb(255, 18, 28, 64),   // #121C40 midnight navy
        Color.FromArgb(255, 9,  16, 40)))  // #091028 deep navy
    {
        g.FillPath(bgGrad, bgPath);
    }

    // Subtle inner glow at top
    using (var glowBrush = new PathGradientBrush(bgPath))
    {
        glowBrush.CenterPoint = new PointF(cx, s * 0.15f);
        glowBrush.CenterColor = Color.FromArgb(35, 0, 200, 255);
        glowBrush.SurroundColors = [Color.FromArgb(0, 0, 200, 255)];
        g.FillPath(glowBrush, bgPath);
    }

    // ── Shield ────────────────────────────────────────────────────────────
    float shW   = s * 0.60f;
    float shH   = s * 0.68f;
    float shTop = cy - shH * 0.52f;

    using var shieldPath = CreateShieldPath(cx, shTop, shW, shH);

    // Shield gradient: bright cyan-teal
    using (var shieldGrad = new LinearGradientBrush(
        new PointF(cx, shTop),
        new PointF(cx, shTop + shH),
        Color.FromArgb(255, 0,  210, 220),   // #00D2DC bright cyan
        Color.FromArgb(255, 0,  140, 160)))  // #008CA0 teal
    {
        g.FillPath(shieldGrad, shieldPath);
    }

    // Shield edge highlight (top-left lighter stroke)
    using (var highlightPen = new Pen(Color.FromArgb(80, 255, 255, 255), s * 0.012f))
    {
        g.DrawPath(highlightPen, shieldPath);
    }

    // ── Inner shield face (lighter inset) ────────────────────────────────
    float inset = s * 0.048f;
    using var innerPath = CreateShieldPath(cx, shTop + inset, shW - inset * 2, shH - inset * 2.2f);
    using (var innerBrush = new SolidBrush(Color.FromArgb(22, 255, 255, 255)))
    {
        g.FillPath(innerBrush, innerPath);
    }

    // ── "LS" text ─────────────────────────────────────────────────────────
    if (size >= 48)
    {
        float fontSize = s * 0.285f;
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var sf   = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        // Text bounds: vertically centered in shield, shift up slightly
        var textRect = new RectangleF(cx - shW / 2f, shTop - s * 0.02f, shW, shH * 0.88f);

        // Drop shadow
        using (var shadow = new SolidBrush(Color.FromArgb(55, 0, 0, 0)))
        {
            var shadowRect = new RectangleF(textRect.X + s * 0.012f, textRect.Y + s * 0.012f,
                                            textRect.Width, textRect.Height);
            g.DrawString("LS", font, shadow, shadowRect, sf);
        }

        // White text
        using var textBrush = new SolidBrush(Color.White);
        g.DrawString("LS", font, textBrush, textRect, sf);
    }
    else if (size >= 32)
    {
        // Small sizes: single bold "L"
        float fontSize = s * 0.42f;
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var sf   = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        var textRect   = new RectangleF(cx - shW / 2f, shTop, shW, shH * 0.9f);
        using var tb   = new SolidBrush(Color.White);
        g.DrawString("L", font, tb, textRect, sf);
    }

    // ── Bottom shield pulse dot ───────────────────────────────────────────
    if (size >= 64)
    {
        float dotR  = s * 0.038f;
        float dotCy = shTop + shH - dotR * 3.8f;
        using var dotBrush = new SolidBrush(Color.FromArgb(255, 60, 255, 180)); // bright green
        g.FillEllipse(dotBrush, cx - dotR, dotCy - dotR, dotR * 2, dotR * 2);

        // Pulse ring around dot
        using var pulsePen = new Pen(Color.FromArgb(130, 60, 255, 180), s * 0.012f);
        float ringR = dotR * 2.2f;
        g.DrawEllipse(pulsePen, cx - ringR, dotCy - ringR, ringR * 2, ringR * 2);
    }

    return bmp;
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

GraphicsPath RoundedRect(RectangleF rect, float radius)
{
    var path = new GraphicsPath();
    float d = radius * 2;
    path.AddArc(rect.X,            rect.Y,            d, d, 180, 90);
    path.AddArc(rect.Right - d,    rect.Y,            d, d, 270, 90);
    path.AddArc(rect.Right - d,    rect.Bottom - d,   d, d,   0, 90);
    path.AddArc(rect.X,            rect.Bottom - d,   d, d,  90, 90);
    path.CloseFigure();
    return path;
}

GraphicsPath CreateShieldPath(float cx, float top, float width, float height)
{
    var path = new GraphicsPath();
    float hw  = width / 2f;
    float bot = top + height;
    float q   = height * 0.25f;

    // Top-left → top-right (flat top)
    path.AddBezier(
        cx - hw, top + q,
        cx - hw, top,
        cx,      top,
        cx,      top);
    path.AddBezier(
        cx,      top,
        cx,      top,
        cx + hw, top,
        cx + hw, top + q);
    // Right side → bottom point
    path.AddBezier(
        cx + hw, top + q,
        cx + hw, top + height * 0.68f,
        cx + hw * 0.38f, top + height * 0.85f,
        cx, bot);
    // Bottom point → left side
    path.AddBezier(
        cx, bot,
        cx - hw * 0.38f, top + height * 0.85f,
        cx - hw, top + height * 0.68f,
        cx - hw, top + q);
    path.CloseFigure();
    return path;
}

// ─── ICO writer ──────────────────────────────────────────────────────────────

void SaveIco(List<Bitmap> bitmaps, string filePath)
{
    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    using var fs = new FileStream(filePath, FileMode.Create);
    using var bw = new BinaryWriter(fs);

    int count = bitmaps.Count;
    bw.Write((short)0);
    bw.Write((short)1);
    bw.Write((short)count);

    var pngDatas = new List<byte[]>();
    foreach (var bmp in bitmaps)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        pngDatas.Add(ms.ToArray());
    }

    int dataOffset = 6 + count * 16;
    for (int i = 0; i < count; i++)
    {
        int w = bitmaps[i].Width >= 256 ? 0 : bitmaps[i].Width;
        int h = bitmaps[i].Height >= 256 ? 0 : bitmaps[i].Height;
        bw.Write((byte)w);
        bw.Write((byte)h);
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write((short)1);
        bw.Write((short)32);
        bw.Write(pngDatas[i].Length);
        bw.Write(dataOffset);
        dataOffset += pngDatas[i].Length;
    }

    foreach (var data in pngDatas)
        bw.Write(data);
}
