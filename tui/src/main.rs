mod app;
mod providers;
mod ui;

use std::io;

use app::App;

fn main() -> io::Result<()> {
    ratatui::run(|terminal| App::new().run(terminal))
}
