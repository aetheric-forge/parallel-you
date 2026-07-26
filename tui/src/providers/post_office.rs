use std::{
    env,
    io,
};

use async_trait::async_trait;
use lapin::{
    Connection,
    ConnectionProperties,
};

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

#[derive(Debug)]
pub struct RabbitMqPostOfficeProvider {
    connection: Connection,
}

impl RabbitMqPostOfficeProvider {
    pub async fn connect(uri: &str) -> ProviderResult<Self> {
        let properties = ConnectionProperties::default()
            .with_connection_name("parallel-you-tui".into())
            .enable_auto_recover();

        let connection =
            Connection::connect(uri, properties).await?;

        Ok(Self { connection })
    }

    pub async fn from_env() -> ProviderResult<Self> {
        let uri = env::var("RABBITMQ_URI")?;

        Self::connect(&uri).await
    }
}

#[async_trait]
impl PostOfficeProvider for RabbitMqPostOfficeProvider {
    fn name(&self) -> &'static str {
        "RabbitMQ Post Office"
    }

    async fn health_check(&self) -> ProviderResult<()> {
        let status = self.connection.status();

        if status.connected() {
            Ok(())
        } else {
            Err(io::Error::new(
                    io::ErrorKind::NotConnected,
                    "RabbitMQ connection is not active",
            )
                .into())
        }
    }
}
