using System.Drawing.Drawing2D;

namespace DkGameOptimizer.Ui;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(10, 16, 26);
    public static readonly Color Sidebar = Color.FromArgb(15, 25, 39);
    public static readonly Color Card = Color.FromArgb(20, 34, 51);
    public static readonly Color Border = Color.FromArgb(38, 57, 76);
    public static readonly Color Accent = Color.FromArgb(42, 213, 190);
    public static readonly Color Text = Color.FromArgb(236, 243, 248);
    public static readonly Color Muted = Color.FromArgb(151, 171, 188);
    public static readonly Color Warning = Color.FromArgb(248, 188, 92);

    public static Font Font(float size, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);

    public static Label Label(string text, int x, int y, int width, int height, float size = 10,
        Color? color = null, FontStyle style = FontStyle.Regular)
        => new()
        {
            Text = text, Location = new Point(x, y), Size = new Size(width, height),
            ForeColor = color ?? Text, Font = Font(size, style), BackColor = Color.Transparent
        };

    public static Button Button(string text, int x, int y, int width, int height, bool primary = false)
    {
        var button = new Button
        {
            Text = text, Location = new Point(x, y), Size = new Size(width, height),
            Font = Font(9.5f, FontStyle.Bold), FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Color.FromArgb(30, 48, 65),
            ForeColor = primary ? Background : Text,
            Cursor = Cursors.Hand, TabStop = true
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(77, 226, 205) : Color.FromArgb(46, 65, 82);
        return button;
    }

    public static TextBox Input(int x, int y, int width, string value = "")
        => new()
        {
            Location = new Point(x, y), Size = new Size(width, 32), Text = value,
            Font = Font(10), BackColor = Color.FromArgb(28, 45, 63),
            ForeColor = Text, BorderStyle = BorderStyle.FixedSingle
        };

    public static Panel CardPanel(int height)
    {
        var card = new Panel
        {
            Width = 890, Height = height, BackColor = Card, Margin = new Padding(0, 0, 0, 16)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        return card;
    }
}
