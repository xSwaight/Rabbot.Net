using Microsoft.Extensions.DependencyInjection;
using Rabbot.Models;
using Sentry.Protocol;
using Serilog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class ImageService
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(ImageService));
        private readonly CacheService _cacheService;

        public ImageService(IServiceProvider services)
        {
            _cacheService = services.GetRequiredService<CacheService>();
        }

        public MemoryStream DrawLevelUp(string name, int level)
        {
            MemoryStream outputStream = new MemoryStream();
            var backgroundImage = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "img", "NeonLevelUp", "LevelUp.png"));
            var levelIcon = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "img", "NeonLevelIcons", $"{level}.png"));

            FontCollection fonts = new FontCollection();
            FontFamily notoSansRegular = fonts.Install(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "fonts", "NotoSans-Regular.ttf"));

            var centerOptions = new TextGraphicsOptions { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            var nameFont = new Font(notoSansRegular, 26, FontStyle.Regular);
            using var image = new Image<Rgba32>(300, 100);
            int fontSize = (int)nameFont.Size;

            //Reduce font size if name is too long
            nameFont = ResizeFont(nameFont, name, 180);


            levelIcon.Mutate(x => x.Resize(80, 80));
            image.Mutate(x => x
                .DrawImage(backgroundImage, new Point(0, 0), 1f)
                .DrawImage(levelIcon, new Point(10, 10), 1f)
                .DrawText(centerOptions, name, nameFont, Color.FromHex("#00FFFF"), new PointF(200, 25))
            );

            image.SaveAsPng(outputStream);
            outputStream.Position = 0;
            return outputStream;
        }

        public async Task<MemoryStream> DrawProfileAsync(UserProfileDto profileInfo, bool isAnimated = false)
        {
            MemoryStream outputStream = new MemoryStream();
            var backgroundImage = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "img", "NeonProfile", "behindProfile.png"));
            var mainImage = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "img", "NeonProfile", "Profile.png"));
            var levelIcon = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "img", "NeonLevelIcons", $"{profileInfo.Level}.png"));
            var expBar = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "img", "NeonProfile", "ExpBar.png"));

            FontCollection fonts = new FontCollection();
            FontFamily notoSansRegular = fonts.Install(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "fonts", "NotoSans-Regular.ttf"));
            FontFamily geometos = fonts.Install(Path.Combine(AppContext.BaseDirectory, "Resources", "RabbotThemeNeon", "assets", "fonts", "Geometos.ttf"));

            var userAvatar = await GetAvatarAsync(profileInfo.AvatarUrl);
            var centerOptions = new TextGraphicsOptions { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var rightOptions = new TextGraphicsOptions { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

            //Fonts
            var nameFont = new Font(notoSansRegular, 24, FontStyle.Regular);
            var levelRankFont = new Font(geometos, 26, FontStyle.Bold);
            var expFont = new Font(geometos, 18, FontStyle.Bold);
            var expInfoFont = new Font(geometos, 11, FontStyle.Bold);
            var goatFont = new Font(geometos, 18, FontStyle.Bold);

            //Gif
            int frameDelay = 0;
            int frameCount = 1;
            List<Image> frames = new List<Image>();
            if (isAnimated)
            {
                frameDelay = userAvatar.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
                frameCount = userAvatar.Frames.Count - 1;
                for (int i = 0; i < frameCount; i++)
                {
                    try
                    {
                        frames.Add(userAvatar.Frames.CloneFrame(i));
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Frame {i} can't be cloned.");
                    }
                }
            }
            else
            {
                frames.Add(userAvatar);
            }

            using (var output = new Image<Rgba32>(300, 175))
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    using var image = new Image<Rgba32>(300, 175);
                    int nameFontSize = (int)nameFont.Size;
                    int expFontSize = (int)expFont.Size;

                    //Reduce font size if name is too long
                    nameFont = ResizeFont(nameFont, profileInfo.Name, 200);

                    //Reduce font size if current exp is too long
                    expFont = ResizeFont(expFont, profileInfo.Exp, 62);

                    float opacity = 1f;
                    var expBarWidth = (int)(161 * (profileInfo.Percent / 100));
                    if (expBarWidth == 0)
                        opacity = 0;

                    image.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = frameDelay;
                    Color color = Color.FromHex("#00FFFF");

                    levelIcon.Mutate(x => x.Resize(27, 27));
                    frames[i].Mutate(x => x.Resize(83, 83));
                    expBar.Mutate(x => x.Resize(expBarWidth, 17));
                    image.Mutate(x => x
                        .DrawImage(backgroundImage, new Point(0, 0), 1f)
                        .DrawImage(frames[i], new Point(10, 10), 1f)
                        .DrawImage(expBar, new Point(119, 130), opacity)
                        .DrawImage(mainImage, new Point(0, 0), 1f)
                        .DrawImage(levelIcon, new Point(80, 80), 1f)
                        .DrawText(centerOptions, profileInfo.Name, nameFont, color, new PointF(195, 25))
                        .DrawText(centerOptions, profileInfo.Rank, levelRankFont, color, new PointF(155, 63))
                        .DrawText(centerOptions, profileInfo.Level, levelRankFont, color, new PointF(239, 63))
                        .DrawText(rightOptions, profileInfo.Exp, expFont, color, new PointF(110, 122))
                        .DrawText(rightOptions, profileInfo.Goats, goatFont, color, new PointF(110, 155))
                        .DrawText(centerOptions, profileInfo.LevelInfo, expInfoFont, color, new PointF(204, 155))
                    );
                    output.Frames.InsertFrame(i, image.Frames.RootFrame);
                }
                if (!isAnimated)
                {
                    output.SaveAsPng(outputStream);
                }
                else
                {
                    output.Frames.RemoveFrame(output.Frames.Count - 1);
                    output.SaveAsGif(outputStream);
                }
            }
            outputStream.Position = 0;
            return outputStream;
        }

        private Font ResizeFont(Font font, string text, int maxWidth, FontStyle fontStyle = FontStyle.Regular)
        {
            int initialFonzSize = (int)font.Size;
            while (true)
            {
                if (TextMeasurer.Measure(text, new RendererOptions(font)).Width > maxWidth)
                {
                    initialFonzSize--;
                    font = new Font(font.Family, initialFonzSize, fontStyle);
                }
                else
                    break;
            }
            return font;
        }

        public async Task<MemoryStream> DrawPettGif(string avatarUrl)
        {
            MemoryStream outputStream = new MemoryStream();
            var pettGif = _cacheService.GetOrAddImage(Path.Combine(AppContext.BaseDirectory, "Resources", "Templates", "assets", "pett.gif"));
            var userAvatar = (await GetAvatarAsync(avatarUrl)).Clone(x => x.ConvertToAvatar(new Size(350, 350), 180));

            int frameDelay = pettGif.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            int frameCount = pettGif.Frames.Count - 1;
            List<Image> frames = new List<Image>();
            for (int i = 0; i < frameCount; i++)
            {
                try
                {
                    frames.Add(pettGif.Frames.CloneFrame(i));
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Frame {i} can't be cloned.");
                }
            }

            using (var output = new Image<Rgba32>(350, 350))
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    using var image = new Image<Rgba32>(350, 350);
                    image.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = frameDelay;
                    image.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).DisposalMethod = GifDisposalMethod.RestoreToBackground;

                    // Animate Avatar
                    userAvatar.Mutate(x => x.Resize(CalculateWidth(i, 230), CalculateHeight(i, 230)));

                    frames[i].Mutate(x => x.Resize(300, 300));
                    image.Mutate(x => x
                        .DrawImage(userAvatar, new Point(50, 70), 1f)
                        .DrawImage(frames[i], new Point(0, 0), 1f));

                    output.Frames.InsertFrame(i, image.Frames.RootFrame);
                }
                output.Frames.RemoveFrame(output.Frames.Count - 1);
                output.SaveAsGif(outputStream);
            }
            outputStream.Position = 0;
            return outputStream;
        }

        private int CalculateWidth(int frameCount, int width)
        {
            int stepSize = 2;
            if(frameCount < 4)
            {
                width -= stepSize * (frameCount + 1);
            }
            else
            {
                width += stepSize * ((frameCount + 1) - 5);
            }

            return width;
        }

        private int CalculateHeight(int frameCount, int height)
        {
            int stepSize = 2;
            if (frameCount < 4)
            {
                height += stepSize * (frameCount + 1);
            }
            else
            {
                height -= stepSize * ((frameCount + 1) - 5);
            }

            return height;
        }

        private async Task<Image> GetAvatarAsync(string url)
        {
            using var webClient = new WebClient();
            using var stream = await webClient.OpenReadTaskAsync(new Uri(url));
            return Image.Load(stream);
        }
    }
}
