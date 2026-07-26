mod app;
mod providers;
mod ui;

use std::io;

use app::App;
use providers::{library::InMemoryLibraryProvider, post_office::InMemoryPostOfficeProvider};

#[tokio::main]
async fn main() -> io::Result<()> {
    let app = App::new(InMemoryLibraryProvider, InMemoryPostOfficeProvider).await;

    let mut terminal = ratatui::init();

    let result = app.run(&mut terminal).await;

    ratatui::restore();

    result
}
