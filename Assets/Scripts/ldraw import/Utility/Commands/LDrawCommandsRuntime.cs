using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace LDraw
{
    
    public enum CommandType
    {
        SubFile = 1,
        Triangle = 3,
        Quad = 4
    }

    public abstract class LDrawCommandRuntime
    {
        protected int _ColorCode = -1;
        protected string _Color;
        
        protected LDrawModelRuntime _Parent;
        public static LDrawCommandRuntime DeserializeCommand(string line, LDrawModelRuntime parent)
        {
            LDrawCommandRuntime command = null;
            int type;
            var args = line.Split(' ');
            if (Int32.TryParse(args[0], out type))
            {
                var commandType = (CommandType)type;
             
                switch (commandType)
                {
                    case CommandType.SubFile:
                        command = new LDrawSubFileRuntime();
                        break;
                    case CommandType.Triangle:
                        command = new LDrawTriangleRuntime();
                        break;
                    case CommandType.Quad:
                        command = new LDrawQuadRuntime();
                        break;
                }
            }
           
            if (command != null)
            {
                if(!int.TryParse(args[1],out command._ColorCode))
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
