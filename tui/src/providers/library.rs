use std::{env, time::Duration};

use async_trait::async_trait;
use mongodb::{Client, bson::doc, options::ClientOptions};

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

#[derive(Debug)]
pub struct MongoLibraryProvider {
    client: Client,
    database_name: String,
}

impl MongoLibraryProvider {
    pub async fn connect(uri: &str, database_name: impl Into<String>) -> ProviderResult<Self> {
        let mut options = ClientOptions::parse(uri).await?;

        // Avoid waiting through MongoDB's longer default timeout
        // when the server is unavailable.
        options.server_selection_timeout = Some(Duration::from_secs(3));

        let client = Client::with_options(options)?;

        Ok(Self {
            client,
            database_name: database_name.into(),
        })
    }

    pub async fn from_env() -> ProviderResult<Self> {
        let uri = env::var("MONGODB_URI")?;

        let database_name =
            env::var("MONGODB_DATABASE").unwrap_or_else(|_| "parallel_you".to_string());

        Self::connect(&uri, database_name).await
    }
}

#[async_trait]
impl LibraryProvider for MongoLibraryProvider {
    fn name(&self) -> &'static str {
        "MongoDB Library"
    }

    async fn health_check(&self) -> ProviderResult<()> {
        self.client
            .database(&self.database_name)
            .run_command(doc! { "ping": 1 })
            .await?;

        Ok(())
    }
}
