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
using System.Drawing;

namespace Diagram {
    public class DiaSynfig {
        public static Diagram build() {
            Color colorSW = Color.DarkRed;
            Color colorCommon = Color.Black;
            Color colorGL = Color.DarkBlue;

            return new Diagram()
                .addBlock(
                    @"glContext",
                    @"Windowless OpenGL context",
                    @"(using EGL)",
                    colorGL,
                    new string[] { "glStorage" }
                )
                .addBlock(
                    @"glStorage",
                    @"Common storage GL resources",
                    @"Store handles of context, shaders, textures in the RendererGL class instance",
                    colorGL,
                    new string[] { "glBlend" }
                )
                .addBlock(
                    @"swBlend",
                    @"«Blend Task»",
                    @"for Software rendering",
                    colorSW,
                    new string[] { "cGroup" }
                )
                .addBlock(
                    @"cGroup",
                    @"Render «Groups» (Layer_PasteCanvas)",
                    @"Build task-trees using «Blend Task» to link tasks of sub-layers.",
                    colorCommon,
                    new string[] { "swLayer" }
                )
                .addBlock(
                    @"glBlend",
                    @"«Blend Task»",
                    @"for OpenGL",
                    colorGL,
                    new string[] { "cAllLayers" }
                )
                .addBlock(
                    @"swLayer",
                    @"«Render Layer Task»",
                    @"for layers which yet not supports new rendering engine (Software)",
                    colorSW,
                    new string[] { "cAllLayers" }
                )
                .addBlock(
                    @"cThreadSafe",
                    @"Thread-safe rendering",
                    @"",
                    colorCommon,
                    new string[] { "cAllLayers", "swMultithread", "glMultithread" }
                )
                .addBlock(
                    @"cAllLayers",
                    @"New rendering for all layers",
                    @"",
                    colorCommon,
                    new string[] { "cFuture" }
                )
                .addBlock(
                    @"swMultithread",
                    @"Multithreaded software rendering",
                    @"We have dependency tree of rendering tasks, so we can run several tasks at same time.",
                    colorSW,
                    new string[] { "cFuture" }
                )
                .addBlock(
                    @"glMultithread",
                    @"Multithreaded OpenGL rendering (optional)",
                    @"There is some restrictions related to hardware acceleration, and multithreading for it will not so effective. PC usually have only one GPU. In best case we can use up to five GPUs - four PCI-x video cards and one integrated video (very expensive!). Also, GPUs uses own memory, so we must remember about transferring data between them.",
                    colorGL,
                    new string[] { "cFuture" }
                )
                .addBlock(
                    @"cFuture",
                    @"Future",
                    @"Abstraction layer to provide access to rendering tasks for script language (python, lua, etc…), so we can build task-tree for renderer via external script. By this way we can write layers at python, and connect it dynamically without restarting of SynfigStudio.",
                    colorCommon
                );
        }
    }
}

