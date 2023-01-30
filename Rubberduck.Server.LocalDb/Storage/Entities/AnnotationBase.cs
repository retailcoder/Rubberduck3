﻿using Rubberduck.Server.LocalDb.Internal;

namespace Rubberduck.DataServer.Storage.Entities
{
    internal abstract class AnnotationBase : DbEntity
    {
        public string AnnotationName { get; set; }
        public string AnnotationArgs { get; set; }

        public int? ContextStartOffset { get; set; }
        public int? ContextEndOffset { get; set; }
        public int? IdentifierStartOffset { get; set; }
        public int? IdentifierEndOffset { get; set; }
    }
}
