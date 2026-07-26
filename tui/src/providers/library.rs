use std::error::Error;

pub type ProviderResult<T> = Result<T, Box<dyn Error + Send + Sync>>;

pub trait LibraryProvider {
    fn name(&self) -> &'static str;
    fn health_check(&self) -> ProviderResult<()>;
}

#[derive(Debug, Default)]
pub struct InMemoryLibraryProvider;

impl LibraryProvider for InMemoryLibraryProvider {
    fn name(&self) -> &'static str {
        "In-Memory Library"
    }

    fn health_check(&self) -> ProviderResult<()> {
        Ok(())
    }
}
