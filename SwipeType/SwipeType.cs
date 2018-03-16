namespace SwipeType
{
    /// <summary>
    ///     Abstract SwipeType.
    /// </summary>
    public abstract class SwipeType
    {
        /// <summary>
        ///     Dictionary of words.
        /// </summary>
        public string[] Words { get; }

        /// <summary>
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        protected SwipeType(string[] wordList)
        {
            Words = wordList;
        }

        /// <summary>
        ///     Returns suggestions for a given inputStr.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public abstract string[] GetSuggestion(string input);
    }
}