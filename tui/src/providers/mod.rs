use std::fmt;

pub mod library;
pub mod post_office;

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum ProviderStatus {
    Available,
    Unavailable(String),
}

impl ProviderStatus {
    pub fn from_result(result: Result<(), Box<dyn std::error::Error + Send + Sync>>) -> Self {
        match result {
            Ok(()) => Self::Available,
            Err(error) => Self::Unavailable(error.to_string()),
        }
    }
}

impl fmt::Display for ProviderStatus {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            ProviderStatus::Available => write!(formatter, "Available"),
            ProviderStatus::Unavailable(error) => {
                write!(formatter, "Unavailable: {error}")
            }
        }
    }
}
