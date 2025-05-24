using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

public static class ImageHelper
{
    public static void GenerateThumbnail(string originalPath, string thumbnailPath, int maxWidth, int maxHeight)
    {
        using var image = Image.Load(originalPath);

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxWidth, maxHeight)
        }));

        // Guardar el thumbnail en formato JPEG con calidad 75 (puedes ajustar)
        image.Save(thumbnailPath, new JpegEncoder
        {
            Quality = 75
        });
    }
}
