﻿using Rubberduck.LanguageServer.Model;
using Rubberduck.UI.Command.SharedHandlers;
using System;

namespace Rubberduck.Editor.Shell.Document.Tabs
{
    /// <summary>
    /// A view model for a type of document tab that contains code for a Visual Basic for Applications 7.0 module/component.
    /// </summary>
    public class VBACodeDocumentTabViewModel : CodeDocumentTabViewModel
    {
        public VBACodeDocumentTabViewModel(Uri documentUri, string title, string content, bool isReadOnly,
            ShowRubberduckSettingsCommand showSettingsCommand,
            CloseToolWindowCommand closeToolWindowCommand)
            : base(documentUri, VisualBasicForApplicationsLanguage.LanguageId, title, content, isReadOnly, showSettingsCommand, closeToolWindowCommand)
        {
        }
    }
}