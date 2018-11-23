namespace SqlSyntaxHighlighting.StringTaggers
{
    enum State
    {
        Default,             // default start state.

        String,              // string ("...")
        InterpolatedString,  // string ($"...")
        MultiLineString,     // multi-line string (@"...")
        InterpolatedMultiLineString,// multi-line string ($@"...")

        MultilineComment,    // /* */
    }
}
