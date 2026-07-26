use std::io;

use crossterm::event::{self, Event, KeyCode, KeyEventKind};
use ratatui::{
    DefaultTerminal,
    Frame,
    widgets::{Block, Borders, Paragraph},
};

fn main() -> io::Result<()> {
    ratatui::run(run)
}

fn run(terminal: &mut DefaultTerminal) -> io::Result<()> {
    loop {
        terminal.draw(render)?;

        if let Event::Key(key) = event::read()? {
            if key.kind == KeyEventKind::Press && key.code == KeyCode::Char('q') {
                return Ok(());
            }
        }
    }
}

fn render(frame: &mut Frame) {
    let shell = Paragraph::new("Parallel You\n\nPress q to quit.")
        .block(
            Block::default()
                .title(" Aetheric Forge ")
                .borders(Borders::ALL),
        );

    frame.render_widget(shell, frame.area());
}
