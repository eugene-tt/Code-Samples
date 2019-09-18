using System;
using System.Collections.Generic;
using System.Text;

namespace KeyboardExtension
{
    //
    //  Direction.swift
    //  TransliteratingKeyboard
    //
    //  Created by Alexei Baboulevitch on 7/19/14.
    //  Copyright (c) 2014 Alexei Baboulevitch ("Archagon"). All rights reserved.
    //

    public enum Direction : int
    {
        Left = 0,
        Down = 3,
        Right = 2,
        Up = 1
    }

    public static class DirectionEx
    {
        public static string description(this Direction f)
        {
            switch (f)
            {
                case Direction.Left:
                        return "Left";
                case Direction.Right:
                        return "Right";
                case Direction.Up:
                        return "Up";
                case Direction.Down:
                        return "Down";
            }
            return "";
        }

        public static Direction clockwise(this Direction f)
        {
            switch (f)
            {
                case Direction.Left:
                    return Direction.Up;
                case Direction.Right:
                    return Direction.Down;
                case Direction.Up:
                    return Direction.Right;
                case Direction.Down:
                    return Direction.Left;
            }
            return Direction.Left;
        }

        public static Direction counterclockwise(this Direction f)
        {
            switch (f)
            {
                case Direction.Left:
                    return Direction.Down;
                case Direction.Right:
                    return Direction.Up;
                case Direction.Up:
                    return Direction.Left;
                case Direction.Down:
                    return Direction.Right;
            }
            return Direction.Left;
        }


        public static Direction opposite(this Direction f)
        {
            switch (f)
            {
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
            }
            return Direction.Left;
        }

        public static bool horizontal(this Direction f)
        {
            switch (f)
            {
                case Direction.Left:
                case Direction.Right:
                    return true;
                default:
                    return false;
            }
        }
    }
}
