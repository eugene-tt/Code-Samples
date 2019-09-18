using System;
using Foundation;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace KeyboardExtension
{
    public enum ShiftState
    {
        Disabled,
        Enabled,
        Locked
    }

    public static class ShiftStates
    {
        public static bool uppercase(this ShiftState f)
        {
            switch(f) 
            {
                case ShiftState.Disabled:
                        return false;
                case ShiftState.Enabled:
                        return true;
                case ShiftState.Locked:
                        return true;
            }
            return false;
        }
    }

    public class Key : NSObject
    {
        public enum KeyType
        {
            Character        ,
            SpecialCharacter ,
            Shift            ,
            Backspace        ,
            ModeChange       ,
            KeyboardChange   ,
            Period           ,
            Space            ,
            Return           ,
            Settings         ,
            Other            ,
        };
        public UIKit.UIColor forceColor = null;
        public KeyType type;
        static int counter = 0;
        public String uppercaseKeyCap = null;
        public String lowercaseKeyCap = null;
        public String uppercaseOutput = null;
        public String lowercaseOutput = null;

        public int toMode;//if the key is a mode button, this indicates which page it links to

        public bool isCharacter
        {
            get
            {
                switch (this.type)
                {
                    case KeyType.Character:
                    case KeyType.SpecialCharacter:
                    case KeyType.Period:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool isSpecial
        {
            get
            {
                switch (this.type)
                { 
                    case KeyType.Shift:
                            return true;
                    case KeyType.Backspace:
                            return true;
                    case KeyType.ModeChange:
                            return true;
                    case KeyType.KeyboardChange:
                            return true;
                    case KeyType.Return:
                            return true;
                    case KeyType.Settings:
                            return true;
                    default:
                            return false;
                }
            }
        }

        public bool hasOutput
        {
            get
            {
                return (this.uppercaseOutput != null) || (this.lowercaseOutput != null);
            }
        }

        // TODO: this is kind of a hack
        int hashValue;

        private void init(KeyType type)
        {
            this.type = type;
            this.hashValue = counter;
            counter += 1;
        }

        public Key(KeyType type)
        {
            init(type);
        }

        public Key(Key key)
        {
            init(key.type);
            this.uppercaseKeyCap = key.uppercaseKeyCap;
            this.lowercaseKeyCap = key.lowercaseKeyCap;
            this.uppercaseOutput = key.uppercaseOutput;
            this.lowercaseOutput = key.lowercaseOutput;
            this.toMode = key.toMode;
        }

        public void setLetter(string letter)
        {
            
            this.lowercaseOutput = letter.ToLower();
            this.uppercaseOutput = letter.ToUpper();
               
            this.lowercaseKeyCap = this.lowercaseOutput;
            this.uppercaseKeyCap = this.uppercaseOutput;
        }

        public string outputForCase(bool uppercase)
        { 
            if (uppercase)
            {
                if (this.uppercaseOutput != null)
                {
                    return this.uppercaseOutput;
                }
                else                 
                if (this.lowercaseOutput != null)
                {
                    return this.lowercaseOutput;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                if (this.lowercaseOutput != null)
                {
                    return this.lowercaseOutput;
                }
                else if (this.uppercaseOutput != null)
                {
                    return this.uppercaseOutput;
                }
                else
                {
                    return "";
                }
            }
        }

        public string keyCapForCase(bool uppercase)
        {
            if (uppercase)
            {
                if(this.uppercaseKeyCap != null)
                {
                    return this.uppercaseKeyCap;
                }
                else if(this.lowercaseKeyCap != null)
                {
                    return this.lowercaseKeyCap;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                if(this.lowercaseKeyCap != null)
                {
                    return this.lowercaseKeyCap;
                }
                else if(this.uppercaseKeyCap != null)
                {
                    return this.uppercaseKeyCap;
                }
                else 
                {
                    return "";
                }
            }
        }
    }

    public class Page
    {
        public List<List<Key>> rows;


        public Page()
        {
            this.rows = new List<List<Key>>();
        }

        public void addKey(Key key, int row)
        {
            if (this.rows.Count <= row)
            {
                for (int i=this.rows.Count; i<=row; i++)
                {
                    this.rows.Add(new List<Key>());
                }
            }

            this.rows[row].Add(key);
        }
    }

    public class Keyboard
    {
        public List<Page> pages;


        public Keyboard()
        {
            this.pages = new List<Page>();
        }

        public void addKey(Key key, int row, int page)
        {
            if (this.pages.Count <= page)
            {
                for (int i = this.pages.Count; i <= page; i++)
                {
                    this.pages.Add(new Page());
                }
            }
            this.pages[page].addKey(key, row);
        }
    }
}