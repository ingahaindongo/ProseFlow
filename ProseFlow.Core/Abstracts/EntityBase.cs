using System.ComponentModel.DataAnnotations;

namespace ProseFlow.Core.Abstracts;

/// <summary>
/// A comprehensive abstract base class for EF Core entities.
/// It includes a generic primary key, auditing properties, and support for
/// soft deletion and optimistic concurrency control.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// The primary key for this entity.
    /// The [Key] attribute marks this property as the primary key for EF Core.
    /// </summary>
    [Key]
    public int Id { get; set; }

    #region Auditing Properties

    /// <summary>
    /// The Coordinated Universal Time (UTC) when the entity was created.
    /// Best practice is to always store dates in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// The Coordinated Universal Time (UTC) when the entity was last updated.
    /// This is nullable because the entity has not been updated upon creation.
    /// </summary>
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    #endregion

    #region Concurrency Control

    /// <summary>
    /// A concurrency token that is automatically updated by the database on every change.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; } = [];

    #endregion
}