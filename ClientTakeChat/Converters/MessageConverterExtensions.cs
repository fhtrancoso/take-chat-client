namespace ClientTakeChat.Converters
{
    public static class MessageConverterExtensions
    {
        /// <summary>
        /// Method to translate the client message to the websocket pattern.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public static string ToChatSocketMessage(this string message, string userName)
        {
            var messageReturn = string.Empty;
            var userTo = string.Empty;

            if (message.Contains(ChatConstants.MESSAGE_TO_INDICATOR_CLIENT))
            {
                var arrays = message.Split(" ");
                userTo = arrays[1];
                message = arrays[2];
                messageReturn = $"{userName}|{userTo}|{message}";
            }
            else
            {
                messageReturn = $"{userName}|{message}";
            }          

            return messageReturn;
        }

        /// <summary>
        /// Method to check if the input is to exit.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsExit(this string message)
        {
            return message.Contains(ChatConstants.MESSAGE_EXIT_CLIENT);
        }
    }
}
