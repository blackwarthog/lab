using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Drawing;

namespace Diagram {
    public class TextUtils {
        static Bitmap tmpBitmap = new Bitmap(10, 10);
        static Graphics tmpGraphics = Graphics.FromImage(tmpBitmap);

        public static string wrap(string text, double width, Font font) {
            string wrapped = "";
            string line = "";
            int wordStart = 0;
            for(int i = 0; i <= text.Length; ++i) {
                char c = i < text.Length ? text[i] : ' ';
                if (char.IsWhiteSpace(c)) {
                    bool newLine = c == '\n';
                    string word = text.Substring(wordStart, i - wordStart);
                    if (word != "") {
                        if (line == "") {
                            line = word;
                            word = "";
                        } else
                        if (tmpGraphics.MeasureString(line, font).Width <= width) {
                            line += " " + word;
                            word = "";
                        } else {
                            newLine = true;
                        }
                    }
                    if (newLine) {
                        if (wrapped != "") wrapped += "\r\n";
                        wrapped += line;
                        line = word;
                    }
                    wordStart = i + 1;
                }
            }

            if (line != "") {
                if (wrapped != "") wrapped += "\r\n";
                wrapped += line;
            }
            return wrapped;
        }

        public static SizeF measure(string text, Font font) {
            return tmpGraphics.MeasureString(text, font);
        }
    }
}

