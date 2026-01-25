using System;
using System.Collections.Generic;
using System.Linq;

namespace ToolProfile
{
    public static class SafeLinkChecker
    {
        // WHITELIST DOMAINS 
        private static HashSet<string> _safeDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Facebook/Meta
            "messenger.com",
            "facebook.com",
            "fb.com",
            "fb.watch",
            "instagram.com",
            "threads.net",
            "whatsapp.com",
            "oculus.com",
            
            // Google
            "google.com",
            "youtube.com",
            "youtu.be",
            "drive.google.com",
            "docs.google.com",
            "gmail.com",
            
            // Microsoft
            "microsoft.com",
            "live.com",
            "office.com",
            "onedrive.com",
            "github.com",
            
            // Trusted platforms
            "discord.com",
            "twitter.com",
            "x.com",
            "reddit.com",
            "twitch.tv",
            "tiktok.com",
            "netflix.com",
            "spotify.com",
            
            // Vietnamese
            "tiktok.com",
            "zingmp3.vn",
            "nhaccuatui.com",
            "nettruyen.com",
            "wikimedia.org",
            "wikipedia.org",
            
            // File sharing
            "dropbox.com",
            "mega.nz",
            "mediafire.com",
            
            // News
            "vnexpress.net",
            "dantri.com",
            "tuoitre.vn",
            "thanhnien.vn"
        };

        // BLACKLIST DOMAINS 
        private static HashSet<string> _dangerousDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Common malicious patterns
            "bit.ly",
            "tinyurl.com",
            "shorturl.at",
            "ow.ly",
            "t.co",
            "is.gd",
            "buff.ly",
            "adf.ly",
            "shorte.st",
            "bc.vc",
            "ouo.io",
            "zzb.bz"
        };

        // EXTENSIONS 
        private static HashSet<string> _dangerousExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".msi", ".bat", ".cmd", ".ps1", ".vbs",
            ".scr", ".jar", ".js", ".hta", ".lnk", ".pif",
            ".com", ".sh", ".bash", ".reg", ".dll", ".sys"
        };

        public static (bool isSafe, string reason) CheckUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return (false, "Empty URL");

            try
            {
                var uri = new Uri(url);
                string domain = uri.Host.ToLower();

                // Remove www.
                if (domain.StartsWith("www."))
                    domain = domain.Substring(4);

                // Check blacklist
                foreach (var dangerous in _dangerousDomains)
                {
                    if (domain.Contains(dangerous))
                        return (false, $"Shortened/risky domain: {dangerous}");
                }

                // Check safe list
                foreach (var safe in _safeDomains)
                {
                    if (domain == safe || domain.EndsWith("." + safe))
                        return (true, $"Trusted domain: {safe}");
                }

                // Check dangerous file extensions
                string path = uri.AbsolutePath.ToLower();
                foreach (var ext in _dangerousExtensions)
                {
                    if (path.EndsWith(ext))
                        return (false, $"Dangerous file type: {ext}");
                }

                // Check IP address (suspicious)
                if (System.Text.RegularExpressions.Regex.IsMatch(domain, @"^\d+\.\d+\.\d+\.\d+$"))
                    return (false, "Direct IP address (suspicious)");

                // Unknown domain - need user confirmation
                return (false, "Unknown domain");
            }
            catch (UriFormatException)
            {
                return (false, "Invalid URL format");
            }
        }

        
        public static void AddSafeDomain(string domain)
        {
            _safeDomains.Add(domain.ToLower());
        }
    }
}