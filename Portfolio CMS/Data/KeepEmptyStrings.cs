using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace PortfolioCms.Data;

/// <summary>Empty form fields stay "" instead of null, so optional text columns never break saving.</summary>
public class KeepEmptyStrings : IDisplayMetadataProvider
{
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (context.Key.ModelType == typeof(string)) context.DisplayMetadata.ConvertEmptyStringToNull = false;
    }
}
