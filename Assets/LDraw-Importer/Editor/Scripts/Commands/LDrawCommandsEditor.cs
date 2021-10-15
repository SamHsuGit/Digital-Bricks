using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace LDraw
{

    public enum CommandTypeEditor
    {
        SubFile = 1,
        Triangle = 3,
        Quad = 4
    }

    public abstract class LDrawCommandsEditor
    {
        protected int _ColorCode = -1;
        protected string _Color;

        protected LDrawModelEditor _Parent;
        public static LDrawCommandsEditor DeserializeCommand(string line, LDrawModelEditor parent)
        {
            LDrawCommandsEditor command = null;
            int type;
            var args = line.Split(' ');
            if (Int32.TryParse(args[0], out type))
            {
                var commandType = (CommandTypeEditor)type;

                switch (commandType)
                {
                    case CommandTypeEditor.SubFile:
                        command = new LDrawSubFileEditor();
                        break;
                    case CommandTypeEditor.Triangle:
                        command = new LDrawTriangleEditor();
                        break;
                    case CommandTypeEditor.Quad:
                        command = new LDrawQuadEditor();
                        break;
                }
            }

            if (command != null)
            {
                if (!int.TryParse(args[1], out command._ColorCode))
                {
                    command._Color = args[1];
                }

                command._Parent = parent;
                command.Deserialize(line);
            }

            return command;
        }

        protected Vector3[] _Verts;
        public abstract void PrepareMeshData(List<int> triangles, List<Vector3> verts);
        public abstract void Deserialize(string serialized);

    }
}
