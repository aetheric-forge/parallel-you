use std::{error::Error, fmt};

pub mod library;
pub mod post_office;

pub type ProviderResult<T> = Result<T, Box<dyn Error + Send + Sync>>;

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum ProviderStatus {
    Available,
    Unavailable(String),
}

impl ProviderStatus {
    pub fn from_result(result: ProviderResult<()>) -> Self {
        match result {
            Ok(()) => Self::Available,
            Err(error) => Self::Unavailable(error.to_string()),
        }
    }
}

impl fmt::Display for ProviderStatus {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::Available => write!(formatter, "Available"),
            Self::Unavailable(error) => {
                write!(formatter, "Unavailable: {error}")
            }
        }
    }
}
