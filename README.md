# PortfolioPilot

PortfolioPilot is a web-based portfolio decision support and simulation tool built in F# with WebSharper.

## Overview

The application helps users compare manually defined investment portfolios using a weighted multi-criteria decision model.  
Instead of relying on live market APIs, it works with manually entered or predefined sample data, which makes it stable, predictable, and suitable for demonstration and educational use.

## Main Idea

Users can:

- define or use sample financial assets
- build portfolios from those assets
- evaluate portfolios across multiple criteria
- adjust decision weights interactively
- view portfolio ranking in real time
- read generated explanations about why a portfolio ranks higher or lower

## Features

- Manual-data portfolio analysis
- Portfolio allocation validation
- Portfolio-level metric calculation
- Multi-criteria decision support
- Benefit/cost normalization
- Weighted portfolio scoring
- Real-time ranking update
- Explanation engine for decision transparency
- Time-based portfolio growth chart
- Interactive long-term simulation (capital, contribution, time horizon)
- Manual asset management (create and remove financial assets)
- Portfolio builder with custom asset allocations
- Weighted decision support system (return, risk, fee, liquidity, diversification)
- Strategy comparison and ranking
- Long-term growth simulation (initial capital, monthly contribution, time horizon)
- Visual comparison (score bars, growth chart, final value comparison)
- Preset scenarios (risk-averse, balanced, growth-focused)

## How to use

1. Adjust decision weights using sliders or preset buttons.
2. Add custom financial assets in the Asset Editor.
3. Create portfolios by assigning asset IDs and allocation percentages.
4. Compare portfolio rankings based on your preferences.
5. Simulate long-term growth using configurable parameters.

## Data model and assumptions

The application uses manually provided asset data instead of live market feeds.

Each asset includes:

- Expected annual return
- Risk score
- Annual fee
- Liquidity score
- Diversification score

Portfolio metrics are calculated using weighted averages of asset properties.

The simulation assumes constant annual returns and monthly compounding.

## Limitations

- No live market data (simulation-based only)
- Simplified risk model (no correlation or covariance)
- Returns are deterministic (no stochastic modeling)

## Future improvements

- Asset selection via dropdown instead of manual ID input
- Data import/export (CSV/JSON)
- Persistent storage (local or database)
- More advanced risk modeling
- Interactive time-series charts

## Current Decision Criteria

The current ranking model uses the following criteria:

- Expected return
- Risk
- Annual fee
- Liquidity
- Diversification

## Tech Stack

- F#
- WebSharper
- ASP.NET Core
- Vite

## Project Structure

```text
src/
  Domain.fs
  Samples.fs
  PortfolioMetrics.fs
  Normalization.fs
  Scoring.fs
  Explanation.fs
  Client.fs
  Startup.fs
```
