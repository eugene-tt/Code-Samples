using System;
using System.Collections.Generic;
using System.Text;

namespace KeyboardExtension
{
    public class DefaultKeyboard: Keyboard
    {
        public DefaultKeyboard()
        {
            Key keyModel = null;

            foreach (var key in new string[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" })
            {
                keyModel = new Key(Key.KeyType.Character);
                keyModel.setLetter(key);
                this.addKey(keyModel, row: 0, page: 0);
            }

            foreach (var key in new string[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" })
            {
                keyModel = new Key(Key.KeyType.Character);
                keyModel.setLetter(key);
                if(key == "A")
                {
                    keyModel.forceColor = UIKit.UIColor.FromRGB(0x89, 0xcf, 0xf0);
                }
                this.addKey(keyModel, row: 1, page: 0);
            }

            keyModel = new Key(Key.KeyType.Shift);
            this.addKey(keyModel, row: 2, page: 0);

            foreach (var key in new string[] { "Z", "X", "C", "V", "B", "N", "M" })
            {
                keyModel = new Key(Key.KeyType.Character);
                keyModel.setLetter(key);
                if (key == "B")
                {
                    keyModel.forceColor = UIKit.UIColor.FromRGB(0xf0, 0x89, 0xcf);
                }
                this.addKey(keyModel, row: 2, page: 0);
            }

            var backspace = new Key(Key.KeyType.Backspace);
            this.addKey(backspace, row: 2, page: 0);

            var keyModeChangeNumbers = new Key(Key.KeyType.ModeChange);
            keyModeChangeNumbers.uppercaseKeyCap = "123";
            keyModeChangeNumbers.toMode = 1;
            this.addKey(keyModeChangeNumbers, row: 3, page: 0);

            var keyboardChange = new Key(Key.KeyType.KeyboardChange);
            this.addKey(keyboardChange, row: 3, page: 0);

            var settings = new Key(Key.KeyType.Settings);
            this.addKey(settings, row: 3, page: 0);

            var space = new Key(Key.KeyType.Space);
            space.uppercaseKeyCap = "space";
            space.uppercaseOutput = " ";
            space.lowercaseOutput = " ";
            this.addKey(space, row: 3, page: 0);

            var returnKey = new Key(Key.KeyType.Return);
            returnKey.uppercaseKeyCap = "return";
            returnKey.uppercaseOutput = "\n";
            returnKey.lowercaseOutput = "\n";
            this.addKey(returnKey, row: 3, page: 0);

            foreach (var key in new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" })
            {
                keyModel = new Key(Key.KeyType.SpecialCharacter);
                keyModel.setLetter(key);
                this.addKey(keyModel, row: 0, page: 1);
            }

            foreach (var key in new string[] { "-", "/", ":", ";", "(", ")", "$", "&", "@", "\"" })
            {
                keyModel = new Key(Key.KeyType.SpecialCharacter);
                keyModel.setLetter(key);
                this.addKey(keyModel, row: 1, page: 1);
            }

            var keyModeChangeSpecialCharacters = new Key(Key.KeyType.ModeChange);
            keyModeChangeSpecialCharacters.uppercaseKeyCap = "#+=";
            keyModeChangeSpecialCharacters.toMode = 2;
            this.addKey(keyModeChangeSpecialCharacters, row: 2, page: 1);

            foreach (var key in new string[] { ".", ",", "?", "!", "'" })
            {
                keyModel = new Key(Key.KeyType.SpecialCharacter);
                keyModel.setLetter(key);
                this.addKey(keyModel, row: 2, page: 1);
            }

            this.addKey(new Key(backspace), row: 2, page: 1);

            var keyModeChangeLetters = new Key(Key.KeyType.ModeChange);
            keyModeChangeLetters.uppercaseKeyCap = "ABC";
            keyModeChangeLetters.toMode = 0;
            this.addKey(keyModeChangeLetters, row: 3, page: 1);

            this.addKey(new Key(keyboardChange), row: 3, page: 1);
            this.addKey(new Key(settings), row: 3, page: 1);
            this.addKey(new Key(space), row: 3, page: 1);
            this.addKey(new Key(returnKey), row: 3, page: 1);

            foreach (var key in new string[] { "[", "]", "{", "}", "#", "%", "^", "*", "+", "=" })
            {
                keyModel = new Key(Key.KeyType.SpecialCharacter);
                keyModel.setLetter(key);
                this.addKey(keyModel, row: 0, page: 2);
            }

            foreach (var key in new string[] { "_", "\\", "|", "~", "<", ">", "€", "£", "¥", "•" })
            {
                keyModel = new Key(Key.KeyType.SpecialCharacter);
                keyModel.setLetter(key);
                this.addKey(keyModel, row: 1, page: 2);
            }

            this.addKey(new Key(keyModeChangeNumbers), row: 2, page: 2);

            foreach (var key in new string[] { ".", ",", "?", "!", "'" })
            {
                keyModel = new Key(Key.KeyType.SpecialCharacter);
                keyModel.setLetter(key);
                this.addKey(keyModel, row: 2, page: 2);
            }

            this.addKey(new Key(backspace), row: 2, page: 2);
            this.addKey(new Key(keyModeChangeLetters), row: 3, page: 2);
            this.addKey(new Key(keyboardChange), row: 3, page: 2);
            this.addKey(new Key(settings), row: 3, page: 2);
            this.addKey(new Key(space), row: 3, page: 2);
            this.addKey(new Key(returnKey), row: 3, page: 2);
        }
    }
}
