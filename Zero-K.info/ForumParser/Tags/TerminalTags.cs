﻿using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Ajax.Utilities;

namespace ZeroKWeb.ForumParser
{
    public abstract class TerminalTag: Tag
    {
        protected StringBuilder sb = new StringBuilder();

        public override string Text => sb.ToString();

        public TerminalTag AppendChar(char c) 
        {
            sb.Append(c);
            return this;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self)
        {
            context.Append(Text);
            return self.Next;
        }
    }

    public class SpaceTag: TerminalTag
    {
        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == ' ' || letter == '\t') { return true;}
            return false;
        }

        public override Tag Create() => new SpaceTag();
    }

    public class NewLineTag: TerminalTag
    {
        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == '\n' || letter == '\r')
            {
                if (sb.ToString().Contains("\n")) return false; // allow only one \n in the same tag
                return true;
            }
            return false;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append("<br/>");
            return self.Next;
        }

        public override Tag Create() => new NewLineTag();
    }


    public class LiteralTag: TerminalTag
    {
        public LiteralTag() {}

        public LiteralTag(string str) {
            sb.Append(str);
        }

        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == ' ' || letter == '\t' || letter == '\r' || letter == '\n') return false;
            return true;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            if (Text.IsValidLink())
            {
                // implicit linkification and imagifination
                if ((Text.EndsWith(".png") || Text.EndsWith(".gif") || Text.EndsWith(".jpg") || Text.EndsWith(".jpeg")) && Text.IsValidLinkOrRelativeUrl(true)) context.AppendFormat("<a href=\"{0}\" target=\"_blank\" ><img src=\"{0}\" max-width=\"100%\" height=\"auto\"/></a>", Text);
                // YouTube auto-embed
                else if (Text.StartsWith("http://www.youtube.com/watch") || Text.StartsWith("https://www.youtube.com/watch"))
                {
                    var m = Regex.Match(Text.Replace("autoplay=1", ""),"v=([^&]+)");
                    if (m.Success)
                    {
                        context.AppendFormat("<iframe width=\"420\" height=\"315\" src=\"https://www.youtube.com/embed/{0}\" frameborder=\"0\" hd=\"1\" allowfullscreen=\"1\"></iframe>", m.Groups[1].Value);
                    }
                }
                else if (Text.StartsWith("http://youtu.be/") || Text.StartsWith("https://youtu.be/"))
                {
                    var m = Regex.Match(Text.Replace("autoplay=1", ""), @"\.be/([^&?]+)");
                    if (m.Success)
                    {
                        context.AppendFormat("<iframe width=\"420\" height=\"315\" src=\"https://www.youtube.com/embed/{0}\" frameborder=\"0\" hd=\"1\" allowfullscreen=\"1\"></iframe>", m.Groups[1].Value);
                    }
                }
                else context.AppendFormat("<a href=\"{0}\">{0}</a>", Text);
            } else context.Append(HttpUtility.HtmlEncode(Text));

            return self.Next;
        }

        public override Tag Create() => new LiteralTag();
    }
}
