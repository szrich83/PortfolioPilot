namespace PortfolioPilot

open WebSharper

[<JavaScript>]
module Samples =

    let sampleAssets : Asset list =
        [
            {
                Id = "asset-sp500"
                Name = "Vanguard S&P 500 ETF"
                Symbol = "VOO"
                Category = ETF
                CurrentPrice = 500.0
                ExpectedAnnualReturn = 8.0
                AnnualVolatility = 15.0
                AnnualFee = 0.03
                LiquidityScore = 9.0
                DiversificationScore = 8.0
                RiskScore = 6.0
            }
            {
                Id = "asset-nasdaq"
                Name = "Invesco Nasdaq-100 ETF"
                Symbol = "QQQ"
                Category = ETF
                CurrentPrice = 450.0
                ExpectedAnnualReturn = 10.0
                AnnualVolatility = 20.0
                AnnualFee = 0.20
                LiquidityScore = 9.0
                DiversificationScore = 7.0
                RiskScore = 8.0
            }
            {
                Id = "asset-world"
                Name = "MSCI World ETF"
                Symbol = "URTH"
                Category = ETF
                CurrentPrice = 150.0
                ExpectedAnnualReturn = 7.0
                AnnualVolatility = 14.0
                AnnualFee = 0.24
                LiquidityScore = 8.0
                DiversificationScore = 9.0
                RiskScore = 5.0
            }
            {
                Id = "asset-bond"
                Name = "Global Bond ETF"
                Symbol = "BNDW"
                Category = Bond
                CurrentPrice = 70.0
                ExpectedAnnualReturn = 4.0
                AnnualVolatility = 6.0
                AnnualFee = 0.10
                LiquidityScore = 8.0
                DiversificationScore = 7.0
                RiskScore = 3.0
            }
            {
                Id = "asset-cash"
                Name = "Cash Reserve"
                Symbol = "CASH"
                Category = Cash
                CurrentPrice = 1.0
                ExpectedAnnualReturn = 2.0
                AnnualVolatility = 1.0
                AnnualFee = 0.0
                LiquidityScore = 10.0
                DiversificationScore = 4.0
                RiskScore = 1.0
            }
        ]

    let samplePortfolios : Portfolio list =
        [
            {
                Id = "portfolio-conservative"
                Name = "Conservative"
                Allocations =
                    [
                        { AssetId = "asset-bond"; Percentage = 50.0 }
                        { AssetId = "asset-world"; Percentage = 30.0 }
                        { AssetId = "asset-cash"; Percentage = 20.0 }
                    ]
            }
            {
                Id = "portfolio-balanced"
                Name = "Balanced"
                Allocations =
                    [
                        { AssetId = "asset-sp500"; Percentage = 45.0 }
                        { AssetId = "asset-world"; Percentage = 25.0 }
                        { AssetId = "asset-bond"; Percentage = 20.0 }
                        { AssetId = "asset-cash"; Percentage = 10.0 }
                    ]
            }
            {
                Id = "portfolio-growth"
                Name = "Growth"
                Allocations =
                    [
                        { AssetId = "asset-nasdaq"; Percentage = 60.0 }
                        { AssetId = "asset-sp500"; Percentage = 30.0 }
                        { AssetId = "asset-world"; Percentage = 10.0 }
                    ]
            }
        ]

    let sampleCriteria : Criterion list =
        [
            { Id = "return"; Name = "Expected Return"; Kind = Benefit; Weight = 35.0 }
            { Id = "risk"; Name = "Risk"; Kind = Cost; Weight = 25.0 }
            { Id = "fee"; Name = "Fee"; Kind = Cost; Weight = 15.0 }
            { Id = "liquidity"; Name = "Liquidity"; Kind = Benefit; Weight = 10.0 }
            { Id = "diversification"; Name = "Diversification"; Kind = Benefit; Weight = 15.0 }
        ]