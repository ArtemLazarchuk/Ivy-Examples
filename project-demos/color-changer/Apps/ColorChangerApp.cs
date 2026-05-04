namespace ColorChanger.Apps;

[App(icon: Icons.Palette, title: "Color Changer")]
public class ColorChangerApp : ViewBase
{
    private static readonly (string Hex, string Name)[] Colors =
    [
        ("#EF4444", "Red"),
        ("#F97316", "Orange"),
        ("#EAB308", "Yellow"),
        ("#22C55E", "Green"),
        ("#3B82F6", "Blue"),
        ("#8B5CF6", "Violet"),
        ("#EC4899", "Pink"),
        ("#14B8A6", "Teal"),
    ];

    public override object? Build()
    {
        var selectedColor = this.UseState(Colors[0]);

        var previewHtml = $"""
            <div style="width:100%;height:200px;border-radius:12px;background:{selectedColor.Value.Hex};transition:background 0.3s ease;"></div>
            """;

        return Layout.Center()
            | (new Card(
                Layout.Vertical().Gap(6).Padding(2).AlignContent(Align.Center)
                | Text.H2("Color Changer")
                | Text.Block("Click a color swatch to change the preview.")
                | new Separator()
                | new Html(previewHtml)
                | Text.H3(selectedColor.Value.Name)
                | Text.Muted(selectedColor.Value.Hex)
                | new Separator()
                | Layout.Horizontal().Gap(2).Wrap().AlignContent(Align.Center)
                    .Children(Colors.Select(c =>
                        new Button(c.Name, onClick: _ => selectedColor.Value = c)
                            .Variant(selectedColor.Value.Hex == c.Hex ? ButtonVariant.Primary : ButtonVariant.Outline)
                    ))
              ).Width(Size.Units(120).Max(480)));
    }
}
