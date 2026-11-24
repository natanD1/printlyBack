# Printly API (backend)

ASP.NET Core 8 + EF Core 8 (Pomelo/MySQL), Clean Architecture: `Printly.Domain` (entidades com setters privados,
`DomainException`), `Printly.Application` (servicos, DTOs, regras de plano), `Printly.Infrastructure` (EF, repositorios,
`UnitOfWork`, DI em `DependencyInjection.cs`), `Printly.WebApi` (controllers, middleware). Testes xUnit em `tests/`
(fakes em memoria em `tests/Printly.Application.Tests/*TestDoubles.cs`; sem Mvc.Testing: hosts Kestrel locais).

## Convencoes

- Mensagens de erro sem acento (excecao: textos que o contrato fixa, ex. "Esse endereço já está em uso.").
- Erros: `DomainException` 400 `{ message }`; `NotFoundException` 404 `{ message }`; `PlanRestrictionException` 403
  `{ message, code }`; `UnauthorizedException` 401; qualquer outra excecao vira 500 `{ "message": "Erro interno." }`
  (log via `ILogger`, sem stack na resposta). Tudo em `ExceptionHandlingMiddleware`.
- Indice unico violado (MySQL 1062) vira `UniqueConstraintException` no `UnitOfWork`; o servico decide a mensagem.
  Application nunca referencia MySqlConnector.
- Tabelas em snake_case (`ToTable("showcase_items")`); colunas e indices ficam no nome padrao do EF.
- So LINQ: nada de SQL cru nem `FromSqlRaw`. Termo de busca vai numa variavel local capturada pela lambda (vira
  parametro). Pra provar, use `CreateDbCommand()` no teste: o `ToQueryString()` do Pomelo mostra string inline so na
  exibicao (o comando real usa `@__x_N`). Migrations antigas nao se editam; nova via
  `dotnet ef migrations add <Nome> --project src/Printly.Infrastructure --startup-project src/Printly.WebApi`.

## Regra de plano nova (receita)

1. `PlanEntitlementKey`: acrescentar **no fim** (gravado como string; nunca renomear).
2. `PlanEntitlementCatalog`: definicao (label/descricao com acento; `Message*` sem acento; unidades de tela nos limites).
3. Servico da acao: `EnsureFeatureAsync` (recurso) ou `EnsureWithinLimitAsync` (limite). Acao que cria varios de uma
   vez usa a sobrecarga com `additional` (valida `usado + additional <= limite`; mensagem diz quantos ainda cabem).
4. Limite com uso: contador em `EntitlementService.CountUsageAsync` (consulta unica no repositorio).
   `GET /plans/me` e `/planos` listam a regra sozinhos (iteram o catalogo). Admin passa em tudo.

## Vitrine publica

- Entidades: `Showcase` (1 por usuario, `Slug` unico varchar(60), `IsPublished`, `IsBlocked` do admin) e
  `ShowcaseItem` (`ProductId`, `SortOrder`, `IsVisible`; unico por vitrine+produto; cascade de vitrine e de produto).
  Nome/preco/foto sempre lidos do `Product` (`Product.UnitSalePrice`, sem coluna). Slug: `ShowcaseSlug`
  (gerado do nome da empresa, sufixo `-2`... em colisao/reservado; nao e editavel: `CompanyProfileService` regera via `ShowcaseSlugResolver` quando o nome da empresa muda,
  no mesmo SaveChanges; link antigo nao redireciona).
- Regras de plano: `ShowcaseEnabled` (escritas do dono; GET e DELETE de item liberados) e `MaxShowcaseItems`
  (conta item visivel com produto produzido). `ShowcasePlanCut` e o corte unico (visivel + produzido, por SortOrder,
  ate o limite): usado no `outsidePlan` do dono, no JSON publico e na foto publica. Downgrade nunca apaga.
- Endpoints: `ShowcaseController` (`/showcase`, `[Authorize]`), `AdminShowcasesController` (`/admin/showcases`,
  `Roles = "Admin"`), `PublicShowcasesController` (`/public/showcases/{slug}`, `[AllowAnonymous]`, slug com constraint
  regex+tamanho; o servico ainda recusa maiusculas, ja que o constraint do ASP.NET ignora caixa).
- Publico: allowlist em `PublicShowcaseDto`; indisponivel = exatamente `{ "available": false }`; contatos passam por
  `PublicContactSanitizer`; imagens por `ImageContent.TryDecode` (magic bytes PNG/JPEG/GIF/WEBP, SVG recusado) com
  `nosniff` e `Cache-Control`.
- Rate limit (`PublicShowcaseRateLimits`, `Program.cs`): politica `public-showcase` = 120 req/min por slug; global =
  1200 req/min por IP remoto so em `/public/`. Limitacao: em producao todo trafego vem do servidor Next (mesmo IP), entao
  o limite por visitante real fica no proxy reverso/CDN. `X-Forwarded-For` nao e lido de proposito (falsificavel).
