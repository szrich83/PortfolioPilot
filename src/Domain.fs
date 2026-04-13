namespace PortfolioPilot

open WebSharper

[<JavaScript>]
type AssetCategory =
    | ETF
    | Stock
    | Bond
    | Cash
    | Crypto

[<JavaScript>]
type Asset =
    {
        Id: string
        Name: string
        Symbol: string
        Category: AssetCategory
        CurrentPrice: float
        ExpectedAnnualReturn: float
        AnnualVolatility: float
        AnnualFee: float
        LiquidityScore: float
        DiversificationScore: float
        RiskScore: float
    }

[<JavaScript>]
type PortfolioAllocation =
    {
        AssetId: string
        Percentage: float
    }

[<JavaScript>]
type Portfolio =
    {
        Id: string
        Name: string
        Allocations: PortfolioAllocation list
    }

[<JavaScript>]
type CriterionKind =
    | Benefit
    | Cost

[<JavaScript>]
type Criterion =
    {
        Id: string
        Name: string
        Kind: CriterionKind
        Weight: float
    }

[<JavaScript>]
type PortfolioMetric =
    {
        ExpectedReturn: float
        Risk: float
        Fee: float
        Liquidity: float
        Diversification: float
    }

[<JavaScript>]
type CriterionScore =
    {
        CriterionId: string
        CriterionName: string
        RawValue: float
        NormalizedValue: float
        WeightedScore: float
    }

[<JavaScript>]
type PortfolioScore =
    {
        PortfolioId: string
        PortfolioName: string
        TotalScore: float
        CriteriaScores: CriterionScore list
        Metrics: PortfolioMetric
    }

[<JavaScript>]
type SimulationInput =
    {
        InitialCapital: float
        MonthlyContribution: float
        Years: int
    }

[<JavaScript>]
type SimulationPoint =
    {
        Month: int
        Value: float
    }