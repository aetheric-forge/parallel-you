use std::error::Error;

pub type ProviderResult<T> = Result<T, Box<dyn Error + Send + Sync>>;

pub trait PostOfficeProvider {
    fn name(&self) -> &'static str;
    fn health_check(&self) -> ProviderResult<()>;
}

#[derive(Debug, Default)]
pub struct InMemoryPostOfficeProvider;

impl PostOfficeProvider for InMemoryPostOfficeProvider {
    fn name(&self) -> &'static str {
        "In-Memory Post Office"
    }

    fn health_check(&self) -> ProviderResult<()> {
        Ok(())
    }
}
