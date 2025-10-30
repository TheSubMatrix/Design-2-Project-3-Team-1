using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace CustomNamespace.Logging
{
    /// <summary>
    /// A custom logging system for Unity, allowing for categorized and level-based logging with file output for errors and warnings.
    /// It provides customizable log settings via ScriptableObject, enabling developers to control which log categories are displayed in the console.
    /// Additionally, it includes utility functions for colorizing text and converting colors to hexadecimal strings.
    /// </summary>
    [CreateAssetMenu(menuName = "Custom Logger")]
    [Serializable]
    public class CustomLogger_SO : ScriptableObject
    {
        //TODO
        //-ADD CATEGORY ONLY VERSIONS
        //-test events to ensure working

        [SerializeField, Tooltip("If enabled, Errors will punch through even disabled Logs.")]
        private bool ErrorPunchThroughEnabled = false;

        [SerializeField, Tooltip("If enabled, all logs will be shown in the console. \nLogs here will NOT be written to file.")]
        private bool SeeAllLogs = false;
        /// <summary>
        /// An array of LogSettings, determining which log categories are displayed.
        /// </summary>
        [SerializeField]
        LogSettings[] m_logSettings;

        /// <summary>
        /// An event that is invoked whenever a log message is deemed visible and is about to be sent to the console.
        /// The event provides the unmodified, original message.
        /// </summary>
        public UnityEvent<string> OnVisibleLog = new();

        /// <summary>
        /// An event that is invoked whenever a log message is deemed visible and is about to be sent to the console.
        /// The event provides the final, formatted string that will be displayed.
        /// </summary>
        public UnityEvent<string> OnVisibleLogWithModifiedMessage = new();

        /// <summary>
        /// A unique hash value used in log file names to differentiate instances.
        /// </summary>
        private static Guid m_hash;

        /// <summary>
        /// Called when the ScriptableObject is loaded or reloaded. Initializes the hash value.
        /// </summary>
        private void OnEnable()
        {
            m_hash = Guid.NewGuid();
        }
        private void OnValidate()
        {
            // Ensure the array is properly initialized
            if (m_logSettings == null || m_logSettings.Length != Enum.GetValues(typeof(LogCategory)).Length)
            {
                m_logSettings = new LogSettings[Enum.GetValues(typeof(LogCategory)).Length];
                //if we need to init it, then we rebuild it
                foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
                {
                    int index = (int)category;
                    m_logSettings[index] = new LogSettings(category, true, false);
                }
            }
        }

        /// <summary>
        /// logs a message with default severity (Info).
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(object message)
        {
            InternalLog(message, message, null, LogLevel.Info);
        }

        /// <summary>
        /// logs a message with default severity (Info).
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="sender">The caller of the log.</param>
        /// <param name="category">The category this log will belong to.</param>
        public void Log(object message, UnityEngine.Object sender = null, LogCategory category = LogCategory.Default)
        {
            InternalLog(message, message, null, LogLevel.Info, category);
        }

        public void Log(
            object message, 
            UnityEngine.Object sender = null, 
            LogLevel level = LogLevel.Info, 
            LogCategory category = LogCategory.Default,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            string messageString = PrepareLogStringWithPlainCallbackInfo(message, file, line);
            string coloredString = PrepareLogStringWithColoredCallbackInfo(message, file, line);
            InternalLog(messageString, coloredString, sender, level, category);
        }

        public void LogWarning(
            object message,
            UnityEngine.Object sender = null,
            LogCategory category = LogCategory.Default,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {

            Log(message,sender, LogLevel.Warning, category, file, line);
        }

        public void LogError(
            object message,
            UnityEngine.Object sender = null,
            LogCategory category = LogCategory.Default,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Log(message, sender, LogLevel.Error, category, file, line);
        }

        /// <summary>
        /// Logs a MemberInfo with specified level, m_category, and sender.
        /// </summary>
        /// <param name="originalMessage">The MemberInfo to log.</param>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="category">The m_category of the log MemberInfo. Defaults to null.</param>
        /// <param name="modifiedMessage"></param>
        /// <param name="sender">The object that sent the log MemberInfo. Defaults to null.</param>
        internal void InternalLog(object originalMessage, object modifiedMessage, UnityEngine.Object sender = null, LogLevel level = LogLevel.Info, LogCategory category = LogCategory.Default)
        {
            int indexOfLogSettings = (int)category;
            if (SeeAllLogs)
            {
                LogInfoAccordingToLevel(originalMessage, modifiedMessage, level, category, sender, false);
                //invoke visible logs to events
                OnVisibleLog.Invoke((string)originalMessage);
                OnVisibleLogWithModifiedMessage.Invoke((string)modifiedMessage);
                return;
            }

            //internal error punch through ensures that the log level is error
            bool errorPunchThrough = ErrorPunchThroughEnabled && level == LogLevel.Error;
            //we want to show logs if they are turned on AND error punch through is not on.
            if (m_logSettings[indexOfLogSettings].ShowLogs == false && !errorPunchThrough) { return; }
            bool logToFile = m_logSettings[indexOfLogSettings].LogToFile;
            LogInfoAccordingToLevel(originalMessage, modifiedMessage, level, category, sender, logToFile);
            //invoke visible logs to events
            OnVisibleLog.Invoke((string)originalMessage);
            OnVisibleLogWithModifiedMessage.Invoke((string)modifiedMessage);
        }

        internal string PrepareLogStringWithColoredCallbackInfo(object message, string file, int line)
        {
            string memberInfo = $"File: {file} | Line: {line}";
            string modifiedMessage = message.ToString();
            //i want to point exceptions better
            memberInfo = ColorText(memberInfo, Color.green);
            if (message is Exception)
            {
                modifiedMessage = ColorText(message.ToString(), Color.red);
            }
            return  $"{memberInfo}\n{modifiedMessage}";
        }

        internal string PrepareLogStringWithPlainCallbackInfo(object message, string file, int line)
        {
            string originalMessage = $"File: {file} | Line: {line}";
            return $"{originalMessage}\n{message}";
        }

        private static void LogInfoAccordingToLevel(object originalMessage, object modifiedMessage, LogLevel level, LogCategory category, UnityEngine.Object sender, bool logToFile)
        {
            switch (level)
            {
                case LogLevel.Error:
#if UNITY_EDITOR
                    Debug.LogError(modifiedMessage, sender);
#endif
                    break;

                case LogLevel.Warning:
#if UNITY_EDITOR
                    Debug.LogWarning(modifiedMessage, sender);
#endif
                    break;

                case LogLevel.Info:
#if UNITY_EDITOR
                    Debug.Log(modifiedMessage, sender);
#endif       
                    break;
            }
            LogIfDebugModeAndAllowed(originalMessage, category, sender, logToFile);
        }

        private static void LogIfDebugModeAndAllowed(object message, LogCategory category, UnityEngine.Object sender, bool logToFile)
        {
            if (Debug.isDebugBuild && logToFile)
            {
                LogErrorToFile(message, sender, category);
            }
        }

        #region Static Extensions

        /// <summary>
        /// Colors a text string with the specified color.
        /// </summary>
        /// <param name="text">The text to color.</param>
        /// <param name="color">The color to apply.</param>
        /// <returns>The colored text string.</returns>
        public static string ColorText(string text, Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGBA(color);
            string output = $"<color=#{hex}>{text}</color>";
            return output;
        }

        /// <summary>
        /// Converts a Color to a hexadecimal string.
        /// </summary>
        /// <param name="c">The Color to convert.</param>
        /// <returns>The hexadecimal string representation of the color.</returns>
        public static string ToHex(Color c)
        {
            return string.Format($"#{ToByte(c.r)}{ToByte(c.g)}{ToByte(c.b)}");
        }

        /// <summary>
        /// Converts a float to a byte, clamping the value between 0 and 1.
        /// </summary>
        /// <param name="f">The float to convert.</param>
        /// <returns>The byte representation of the float.</returns>
        static byte ToByte(float f)
        {
            f = Mathf.Clamp01(f);
            return (byte)(f * 255);
        }

        #endregion

        /// <summary>
        /// Logs an error MemberInfo to a file.
        /// </summary>
        /// <param name="message">The error MemberInfo to log.</param>
        /// <param name="sender">The object that sent the error MemberInfo.</param>
        /// <param name="category">The m_category of the log MemberInfo. Defaults to Default.</param>
        public static void LogErrorToFile(object message, UnityEngine.Object sender, LogCategory category = LogCategory.Default)
        {
            string timestampDate = DateTime.Now.ToString("yyyy-MM-dd");
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss.fff");
            // ReSharper disable once StringLiteralTypo
            string logDir = Application.dataPath + "/CUSTOMLOGS";
            string filePath = logDir + $"/error_log_{timestampDate}_{m_hash}.txt";
            string currentSceneName = SceneManager.GetActiveScene().name;

            // Ensure the directory exists
            if (!Directory.Exists(logDir))
            {
                if(Debug.isDebugBuild)
                {
                    Debug.Log("creating directory");
                }
                Directory.CreateDirectory(logDir);
            }

            // Create file if it doesn't exist
            if (!File.Exists(filePath))
            {
                using StreamWriter writer = File.CreateText(filePath);
                string senderName = sender?.name ?? "N/A";
                if (sender == null)
                {
                    senderName = "File Creator Name Not Defined";
                }
                writer.WriteLine($"[File Creator: {senderName}]"); // Optional: Add a header for the new file
            }

            // Append the log entry
            using (StreamWriter writer = File.AppendText(filePath))
            {
                writer.WriteLine($"[{timestamp}] Source: {sender}, Logger: {category}, Scene: {currentSceneName}\n {message}");
            }
        }

        /// <summary>
        /// Enumeration of log categories.
        /// </summary>
        [Serializable]
        public enum LogCategory
        {
            //Basics
            Default,
            Other,
            // ReSharper disable once IdentifierTypo
            Nulled,
            //Common
            UI,
            Performance,
            System,
            Audio,
            Networking,
            Input,
            //Player
            Enemy,
            Player,
            //Environment
            Environment,
            Animation,
            Physics,
            VFX,
            //Systems
            AI,
            Abilities,
            Inventory,
            SaveLoad,
            WorldGen,
            Missions
        }

        /// <summary>
        /// Enumeration of log levels.
        /// </summary>
        [Serializable]
        public enum LogLevel
        {
            Error,      //only errors
            Warning,    //only warnings
            Info        //only standard log info
        }

        /// <summary>
        /// Structure representing log settings for a m_category.
        /// </summary>
        [Serializable]
        public struct LogSettings
        {
            [HideInInspector, SerializeField]
            private string m_category;
            /// <summary>
            /// The m_category of the log settings.
            /// </summary>
            [HideInInspector][SerializeField]
            public LogCategory Category; 

            /// <summary>
            /// Whether logs of this m_category should be displayed.
            /// </summary>
            [SerializeField]
            public bool ShowLogs, LogToFile;

            /// <summary>
            /// Constructor for LogSettings.
            /// </summary>
            /// <param name="category">The log m_category.</param>
            /// <param name="showLogs">Whether to show logs for this m_category.</param>
            /// <param name="logToFile">Whether this should log to a file</param>
            public LogSettings(LogCategory category, bool showLogs, bool logToFile)
            {
                Category = category;
                ShowLogs = showLogs;
                LogToFile = logToFile;
                this.m_category = Category.ToString();
            }
        }
    }
}