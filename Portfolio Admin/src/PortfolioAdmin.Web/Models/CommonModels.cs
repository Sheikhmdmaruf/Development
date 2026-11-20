namespace PortfolioAdmin.Web.Models;

public record PagerModel(int Page, int Pages, int From, int To, int Total);

public record SearchItem(string Title, string Sub, string Url, string Icon);
public record SearchGroup(string Label, List<SearchItem> Items);

public record ReorderRequest(int[] Ids);
