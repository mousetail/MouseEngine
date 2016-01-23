﻿namespace MouseEngine.Lowlevel
{
    partial class Phrase
    {
        /// <summary>
        /// The phrase to return stuff
        /// </summary>
        public static Phrase returnf = new Phrase(new Argument[] { new Argument("value", ClassDatabase.integer) }, null, new MultiStringMatcher(new string[1] { "value" }, "return ", ""),
                new Opcode(opcodeType.returnf, new IArgItem[1] { new ArgItemFromArguments() }));
        public static Phrase push = new Phrase(new[] { new Argument("value", ClassDatabase.integer) }, null, new MultiStringMatcher(new[] { "value" }, new[] { "push ", "" }), new Opcode(opcodeType.copy, new IArgItem[] { new ArgItemFromArguments(), ArgumentValue.Push }));

        public static Phrase add = new Phrase(new[] { new Argument("left", ClassDatabase.integer), new Argument("right", ClassDatabase.integer) }, ClassDatabase.integer,
            new MultiStringMatcher(new[] { "left", "right" }, "", "+", ""), new Opcode(opcodeType.add, new IArgItem[] { new ArgItemFromArguments(), new ArgItemFromArguments(), new ArgItemReturnValue() }));

        public static Phrase assign = new Phrase(new[] { new Argument("to", ClassDatabase.integer), new Argument("from", ClassDatabase.integer) }, null, new VoidMather(),
            new Opcode(opcodeType.copy, new IArgItem[] { new ArgItemFromArguments(), new ArgItemFromArguments() }));

        public static Phrase makeWindow = new Phrase(new[] {
            Argument.fromStack("parent", ClassDatabase.integer),
            Argument.fromStack("method",ClassDatabase.integer),
            Argument.fromStack("size",ClassDatabase.integer),
            Argument.fromStack("type",ClassDatabase.integer),
            Argument.fromStack("rock",ClassDatabase.integer),}, ClassDatabase.integer,
            new MultiStringMatcher(new[] { "parent", "type", "size", "method", "rock" }, "split ", " into ", " of size ", " using ", " and rock ", ""),
            new Opcode(opcodeType.glk, new IArgItem[] {
                new ArgumentValue(addressMode.constint, (int)glkFunction.glk_window_open),
                new ArgumentValue(addressMode.constint, 5), new ArgItemReturnValue(),

            }));
        public static Phrase setIOSystem = new Phrase(new[] { new Argument("system", ClassDatabase.integer) }, null,
            new MultiStringMatcher(new[] { "system" }, "set IO system to ", ""),
            new Opcode(opcodeType.setiosys,new IArgItem[] { new ArgItemFromArguments(), new ArgumentValue(addressMode.zero) }));

        public static Phrase setIOWindow = new Phrase(new[]
        {
            Argument.fromStack ("window",ClassDatabase.integer)
        }, null, new MultiStringMatcher(new[] { "window" }, "set the default window to ", "" ),
            new Opcode(opcodeType.glk, new IArgItem[] {
                new ArgumentValue(addressMode.constint, (int)glkFunction.glk_set_window),
                new ArgumentValue(addressMode.constint,1),
                new ArgumentValue(addressMode.zero)
            }));

        public static Phrase printUniChar = new Phrase(new[] { new Argument("char", ClassDatabase.integer) },
            null, new MultiStringMatcher(new[] { "char" }, "print unicode char ","" ),
            new Opcode(opcodeType.streamunichar, new IArgItem[] { new ArgItemFromArguments() }));

        public static Phrase GiveError = new Phrase(new Argument[0], null, new StringMatcher("error"),
            new Opcode(opcodeType.debugtrap, new IArgItem[0]));

        public static Phrase GlkPoll = new Phrase(new Argument[0], ClassDatabase.integer, new StringMatcher("poll"),
            new Opcode(opcodeType.copy, new ArgumentValue(addressMode.constint,256), ArgumentValue.Push),
            new Opcode(opcodeType.glk, new ArgumentValue(addressMode.constint, (int)glkFunction.glk_select_poll),
                new ArgumentValue(addressMode.constint,1), new ArgItemReturnValue()));

        public static Phrase IOprint = new Phrase(new Argument[1] { new Argument("text", ClassDatabase.str) },
            null, new MultiStringMatcher(new[] { "text" }, "say ", ""),
            new Opcode(opcodeType.streamstr,new ArgItemFromArguments()));
    }
}