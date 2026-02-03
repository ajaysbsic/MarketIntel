namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ICategoryClassifier
{
    Task<(string Category, string Summary, double Confidence)> ClassifyAndSummarizeAsync(
        string title, 
        string bodyText);
}
