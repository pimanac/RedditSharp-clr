﻿using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RedditSharp.Things;

namespace RedditSharp
{
   public class SubredditSettings
   {
      private const string SiteAdminUrl = "/api/site_admin";
      private const string DeleteHeaderImageUrl = "/api/delete_sr_header";

      private Reddit Reddit { get; set; }
      private IWegAgent WebAgent { get; set; }

      [JsonIgnore]
      public Subreddit Subreddit { get; set; }

      public SubredditSettings(Reddit reddit, Subreddit subreddit, IWegAgent webAgent)
      {
         Subreddit = subreddit;
         Reddit = reddit;
         WebAgent = webAgent;
         // Default settings, for use when reduced information is given
         AllowAsDefault = true;
         Domain = null;
         Sidebar = string.Empty;
         Language = "en";
         Title = Subreddit.DisplayName;
         WikiEditKarma = 100;
         WikiEditAge = 10;
         UseDomainCss = false;
         UseDomainSidebar = false;
         HeaderHoverText = string.Empty;
         NSFW = false;
         PublicDescription = string.Empty;
         WikiEditMode = WikiEditMode.None;
         SubredditType = SubredditType.Public;
         ShowThumbnails = true;
         ContentOptions = ContentOptions.All;
         SpamFilter = new SpamFilterSettings();
      }

      public SubredditSettings(Subreddit subreddit, Reddit reddit, JObject json, IWegAgent webAgent)
         : this(reddit, subreddit, webAgent)
      {
         var data = json["data"];
         AllowAsDefault = data["default_set"].ValueOrDefault<bool>();
         Domain = data["domain"].ValueOrDefault<string>();
         Sidebar = HttpHelper.HtmlDecode(data["description"].ValueOrDefault<string>() ?? string.Empty);
         Language = data["language"].ValueOrDefault<string>();
         Title = data["title"].ValueOrDefault<string>();
         WikiEditKarma = data["wiki_edit_karma"].ValueOrDefault<int>();
         UseDomainCss = data["domain_css"].ValueOrDefault<bool>();
         UseDomainSidebar = data["domain_sidebar"].ValueOrDefault<bool>();
         HeaderHoverText = data["header_hover_text"].ValueOrDefault<string>();
         NSFW = data["over_18"].ValueOrDefault<bool>();
         PublicDescription = HttpHelper.HtmlDecode(data["public_description"].ValueOrDefault<string>() ?? string.Empty);
         SpamFilter = new SpamFilterSettings
         {
            LinkPostStrength = GetSpamFilterStrength(data["spam_links"].ValueOrDefault<string>()),
            SelfPostStrength = GetSpamFilterStrength(data["spam_selfposts"].ValueOrDefault<string>()),
            CommentStrength = GetSpamFilterStrength(data["spam_comments"].ValueOrDefault<string>())
         };
         if (data["wikimode"] != null)
         {
            var wikiMode = data["wikimode"].ValueOrDefault<string>();
            switch (wikiMode)
            {
               case "disabled":
                  WikiEditMode = WikiEditMode.None;
                  break;
               case "modonly":
                  WikiEditMode = WikiEditMode.Moderators;
                  break;
               case "anyone":
                  WikiEditMode = WikiEditMode.All;
                  break;
            }
         }
         if (data["subreddit_type"] != null)
         {
            var type = data["subreddit_type"].ValueOrDefault<string>();
            switch (type)
            {
               case "public":
                  SubredditType = SubredditType.Public;
                  break;
               case "private":
                  SubredditType = SubredditType.Private;
                  break;
               case "restricted":
                  SubredditType = SubredditType.Restricted;
                  break;
            }
         }
         ShowThumbnails = data["show_media"].ValueOrDefault<bool>();
         WikiEditAge = data["wiki_edit_age"].ValueOrDefault<int>();
         if (data["content_options"] != null)
         {
            var contentOptions = data["content_options"].ValueOrDefault<string>();
            switch (contentOptions)
            {
               case "any":
                  ContentOptions = ContentOptions.All;
                  break;
               case "link":
                  ContentOptions = ContentOptions.LinkOnly;
                  break;
               case "self":
                  ContentOptions = ContentOptions.SelfOnly;
                  break;
            }
         }
      }

      public bool AllowAsDefault { get; set; }
      public string Domain { get; set; }
      public string Sidebar { get; set; }
      public string Language { get; set; }
      public string Title { get; set; }
      public int WikiEditKarma { get; set; }
      public bool UseDomainCss { get; set; }
      public bool UseDomainSidebar { get; set; }
      public string HeaderHoverText { get; set; }
      public bool NSFW { get; set; }
      public string PublicDescription { get; set; }
      public WikiEditMode WikiEditMode { get; set; }
      public SubredditType SubredditType { get; set; }
      public bool ShowThumbnails { get; set; }
      public int WikiEditAge { get; set; }
      public ContentOptions ContentOptions { get; set; }
      public SpamFilterSettings SpamFilter { get; set; }

      public void UpdateSettings()
      {
         string link_type;
         string type;
         string wikimode;
         switch (ContentOptions)
         {
            case ContentOptions.All:
               link_type = "any";
               break;
            case ContentOptions.LinkOnly:
               link_type = "link";
               break;
            default:
               link_type = "self";
               break;
         }
         switch (SubredditType)
         {
            case SubredditType.Public:
               type = "public";
               break;
            case SubredditType.Private:
               type = "private";
               break;
            default:
               type = "restricted";
               break;
         }
         switch (WikiEditMode)
         {
            case WikiEditMode.All:
               wikimode = "anyone";
               break;
            case WikiEditMode.Moderators:
               wikimode = "modonly";
               break;
            default:
               wikimode = "disabled";
               break;
         }
         var data = new
         {
            allow_top = AllowAsDefault,
            description = Sidebar,
            domain = Domain,
            lang = Language,
            link_type,
            over_18 = NSFW,
            public_description = PublicDescription,
            show_media = ShowThumbnails,
            sr = Subreddit.FullName,
            title = Title,
            type,
            uh = Reddit.User.Modhash,
            wiki_edit_age = WikiEditAge,
            wiki_edit_karma = WikiEditKarma,
            wikimode,
            spam_links = SpamFilter == null ? null : SpamFilter.LinkPostStrength.ToString().ToLowerInvariant(),
            spam_selfposts = SpamFilter == null ? null : SpamFilter.SelfPostStrength.ToString().ToLowerInvariant(),
            spam_comments = SpamFilter == null ? null : SpamFilter.CommentStrength.ToString().ToLowerInvariant(),
            api_type = "json"
         };

        var response = WebAgent.Post(SiteAdminUrl, data, "header-title", HeaderHoverText);

      }

      /// <summary>
      /// Resets the subreddit's header image to the Reddit logo
      /// </summary>
      public void ResetHeaderImage()
      {
         var data = new
         {
            uh = Reddit.User.Modhash,
            r = Subreddit.Name
         };

         var response = WebAgent.Post(DeleteHeaderImageUrl, data);
      }

      private SpamFilterStrength GetSpamFilterStrength(string rawValue)
      {
         switch (rawValue)
         {
            case "low":
               return SpamFilterStrength.Low;
            case "high":
               return SpamFilterStrength.High;
            case "all":
               return SpamFilterStrength.All;
            default:
               return SpamFilterStrength.High;
         }
      }
   }

   public enum WikiEditMode
   {
      None,
      Moderators,
      All
   }

   public enum SubredditType
   {
      Public,
      Restricted,
      Private
   }

   public enum ContentOptions
   {
      All,
      LinkOnly,
      SelfOnly
   }

   public enum SpamFilterStrength
   {
      Low,
      High,
      All
   }
}