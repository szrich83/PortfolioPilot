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
