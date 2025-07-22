using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NotificationBanner.Util {
    internal static class Extensions {
        private const string Base64Prefix = "data:image/";
        internal static Image ImageFromBase64(this string base64String) {
            base64String = base64String.Split(";base64,").Last(); // base64String.Replace(Base64Prefix, "");
            var converted = Convert.FromBase64String(base64String);
            using (MemoryStream ms = new MemoryStream(converted)) return Image.FromStream(ms);
        }
        internal static Task<Image> GetImageAsync(this Uri uri) {
            using (var httpClient = new HttpClient()) {
                var byteArray = httpClient.GetByteArrayAsync(uri).Result;
                return Task.FromResult(Image.FromStream(new MemoryStream(byteArray)));
            }
        }
        internal static Image? ParseImage(this string input) {
            if (input.StartsWith(Base64Prefix, StringComparison.OrdinalIgnoreCase)) {
                try { return ImageFromBase64(input); } catch (Exception ex) { Console.WriteLine(ex.Message); return null; }
            }
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) {
                try { return GetImageAsync(uri).Result; } catch (Exception ex) { Console.WriteLine(ex.Message); return null; }
            }
            return null;
        }
        public static Image Resize(this Image imgToResize, Size size) => new Bitmap(imgToResize, size) as Image;
    }
}
