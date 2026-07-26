mod app;
mod providers;
mod ui;

use std::io;

use app::App;
use providers::{
    library::MongoLibraryProvider,
    post_office::RabbitMqPostOfficeProvider,
};

#[tokio::main]
async fn main() -> io::Result<()> {
    let library = MongoLibraryProvider::from_env()
        .await
        .map_err(|error| io::Error::other(error.to_string()))?;

    let post_office =
        RabbitMqPostOfficeProvider::from_env()
        .await
        .map_err(|error| {
            io::Error::other(error.to_string())
        })?;

    let app = App::new(library, post_office).await;

    let mut terminal = ratatui::init();

    let result = app.run(&mut terminal).await;

    ratatui::restore();

    result
}
