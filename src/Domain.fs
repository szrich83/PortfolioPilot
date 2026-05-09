namespace PortfolioPilot

open WebSharper

// Supported asset categories used by the portfolio model
[<JavaScript>]
type AssetCategory =
    | ETF
    | Stock
    | Bond
    | Cash
    | Crypto

// Represents one manually defined financial asset
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

// Connects an asset to a portfolio with a percentage allocation
[<JavaScript>]
type PortfolioAllocation =
    {
        AssetId: string
        Percentage: float
    }

// Represents an investment strategy composed of multiple allocations
[<JavaScript>]
type Portfolio =
    {
        Id: string
        Name: string
        Allocations: PortfolioAllocation list
    }

// Defines whether a criterion should reward higher or lower values
[<JavaScript>]
type CriterionKind =
    | Benefit
    | Cost

// Decision criterion used by the weighted scoring model
[<JavaScript>]
type Criterion =
    {
        Id: string
        Name: string
        Kind: CriterionKind
        Weight: float
    }

// Aggregated metrics calculated from portfolio allocations
[<JavaScript>]
type PortfolioMetric =
    {
        ExpectedReturn: float
        Risk: float
        Fee: float
        Liquidity: float
        Diversification: float
    }

// Stores the normalized and weighted score of a single criterion
[<JavaScript>]
type CriterionScore =
    {
        CriterionId: string
        CriterionName: string
        RawValue: float
        NormalizedValue: float
        WeightedScore: float
    }

// Final portfolio ranking result
[<JavaScript>]
type PortfolioScore =
    {
        PortfolioId: string
        PortfolioName: string
        TotalScore: float
        CriteriaScores: CriterionScore list
        Metrics: PortfolioMetric
    }

// User input for long-term growth simulation
[<JavaScript>]
type SimulationInput =
    {
        InitialCapital: float
        MonthlyContribution: float
        Years: int
    }

// One data point in the simulated portfolio growth timeline
[<JavaScript>]
type SimulationPoint =
    {
        Month: int
        Value: float
    }

// Data structure used for JSON export/import
[<JavaScript>]
type ExportData =
    {
        Assets: Asset list
        Portfolios: Portfolio list
    }