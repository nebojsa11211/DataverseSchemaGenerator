using CommunityToolkit.Mvvm.ComponentModel;
using DataverseSchemaGenerator.Core.Models;

namespace DataverseSchemaGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel representing a Dataverse entity for display and selection.
/// </summary>
public partial class EntityViewModel : ObservableObject
{
    private readonly EntityMetadata _entity;

    [ObservableProperty]
    private bool _isSelected = true;

    public EntityViewModel(EntityMetadata entity)
    {
        _entity = entity;
    }

    public string LogicalName => _entity.LogicalName;

    public string DisplayName => _entity.DisplayName ?? _entity.LogicalName;

    public int AttributeCount => _entity.Attributes.Count;

    public string Description => _entity.Description ?? string.Empty;

    public EntityMetadata Entity => _entity;

    public string DisplayText => $"{DisplayName} ({AttributeCount} attributes)";
}
