namespace ProseFlow.Application.DTOs.Dashboard;

public record DailyUsageDto(DateOnly Date, long PromptTokens, long CompletionTokens, double TokensPerSecond);