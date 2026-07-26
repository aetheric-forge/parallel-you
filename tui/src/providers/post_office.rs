use async_trait::async_trait;

use super::ProviderResult;

#[async_trait]
pub trait PostOfficeProvider: Send + Sync {
    fn name(&self) -> &'static str;

    async fn health_check(&self) -> ProviderResult<()>;
}

#[derive(Debug, Default)]
pub struct InMemoryPostOfficeProvider;

#[async_trait]
impl PostOfficeProvider for InMemoryPostOfficeProvider {
    fn name(&self) -> &'static str {
        "In-Memory Post Office"
    }

    async fn health_check(&self) -> ProviderResult<()> {
        Ok(())
    }
}
