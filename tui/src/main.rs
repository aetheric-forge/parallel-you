mod app;
mod providers;
mod ui;

use std::io;

use app::App;
use providers::{library::InMemoryLibraryProvider, post_office::InMemoryPostOfficeProvider};

fn main() -> io::Result<()> {
    let app = App::new(InMemoryLibraryProvider, InMemoryPostOfficeProvider);

    ratatui::run(|terminal| app.run(terminal))
}
