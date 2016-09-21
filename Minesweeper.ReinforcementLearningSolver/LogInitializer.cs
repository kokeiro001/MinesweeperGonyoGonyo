using log4net;

namespace Minesweeper.ReinforcementLearningSolver
{
    static class LogInitializer
    {
        public static void InitLog(string loggerName, string outputLogFileName)
        {
            var ilogger = LogManager.GetLogger(loggerName);

            var layout = new log4net.Layout.PatternLayout(@"%-5level %date{yyyy/MM/dd_HH:mm:ss,fff} [%thread] %logger - %message%newline");
            
            var fileAppender = new log4net.Appender.FileAppender()
            {
                Layout = layout,
                File = outputLogFileName,
                AppendToFile = true,
                
            };
            
            var consoleAppender = new log4net.Appender.ConsoleAppender()
            {
                Layout = layout,
            };
            var debugAppender = new log4net.Appender.DebugAppender()
            {
                Layout = layout,
            };
            var logger = ilogger.Logger as log4net.Repository.Hierarchy.Logger;
            logger.Level = log4net.Core.Level.All;
            logger.AddAppender(fileAppender);
            logger.AddAppender(consoleAppender);
            logger.AddAppender(debugAppender);
            
            logger.Repository.Configured = true;

            fileAppender.ActivateOptions();
            consoleAppender.ActivateOptions();
            debugAppender.ActivateOptions();
        }
    }
}
