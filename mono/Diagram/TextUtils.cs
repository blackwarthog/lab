/*
    ......... 2015 Ivan Mahonin

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

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

