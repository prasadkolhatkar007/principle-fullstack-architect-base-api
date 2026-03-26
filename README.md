# Principle Fullstack Architect Base API
A high-performance, enterprise-grade foundational API built on .NET 9 using Minimal APIs. This repository serves as a blueprint for scalable Microservices, implementing Clean Architecture, CQRS, and Domain-Driven Design (DDD) to reduce technical debt and maximize system deployment speed.


### Tech Stack
.NET 9


### Clone Project
git clone https://github.com/prasadkolhatkar007/principle-fullstack-architect-base-api.git
cd principle-fullstack-architect-base-api

### Run Project
dotnet restore
dotnet run --project PrincipleFsa.BaseApi

##### Verify run 
/health
or at root (/) shows message {"service":"Principle Fullstack Architect Base API","workItem":"Repo setup & Architecture Design (Minimal APIs)"}

##### Day - 25Mar2026 
feat(setup): initialize .NET 9 Minimal API with global exception handling and health checks

##### Day - 26Mar2026 
feat(web): initialize React 19 frontend with Vite and TanStack Query

