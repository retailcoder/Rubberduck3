using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.Unmanaged.Registration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Declarations.Types;

/// <summary>
/// An object type that can be iterated in a <c>For Each...Next</c> loop.
/// </summary>
public record class VBCollectionType : VBClassType, IEnumerableType
{
    public VBCollectionType(VBClassType vbClass)
        : this(vbClass.Name, vbClass.Uri, vbClass.IsUserDefined, vbClass.Members, vbClass.Members.OfType<VBReturningMember>().Single(e => e.UserMemId == WellKnownDispIds.NewEnum)) { }

    public VBCollectionType(string name, Uri uri, bool isUserDefined = false, IEnumerable<VBTypeMember>? members = null, VBReturningMember? newEnumMember = null)
        : base(name, uri, isUserDefined, members)
    {
        NewEnumMember = newEnumMember;
    }

    public bool IsArray { get; } = false;
    /// <summary>
    /// The member that provides the enumerator for this collection.
    /// </summary>
    /// <remarks>
    /// Controlled by the <c>VB_UserMemId</c> attribute with a value of <c>-4</c> or the <c>@NewEnum</c> annotation.
    /// </remarks>
    public VBReturningMember? NewEnumMember { get; init; }
}
