use async_trait::async_trait;

use super::ProviderResult;

#[async_trait]
pub trait LibraryProvider: Send + Sync {
    fn name(&self) -> &'static str;

    async fn health_check(&self) -> ProviderResult<()>;
}

#[derive(Debug, Default)]
pub struct InMemoryLibraryProvider;

#[async_trait]
impl LibraryProvider for InMemoryLibraryProvider {
    fn name(&self) -> &'static str {
        "In-Memory Library"
    }

    async fn health_check(&self) -> ProviderResult<()> {
        Ok(())
    }
}
